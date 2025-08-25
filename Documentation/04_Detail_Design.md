# Detail Design Document
## EduSmartAI Backend System

### 1. Class Design Specifications

#### 1.1 Authentication Service Classes

##### 1.1.1 Domain Entities
```csharp
// Domain Entity: Account
namespace AuthService.Domain.WriteModels
{
    public partial class Account
    {
        public Guid AccountId { get; set; }
        public Guid RoleId { get; set; }
        public string Email { get; set; } = null!;
        public bool EmailConfirmed { get; set; }
        public string? PasswordHash { get; set; }
        public DateTimeOffset? LockoutEnd { get; set; }
        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; } = null!;
        public bool IsActive { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string UpdatedBy { get; set; } = null!;
        public int AccessFailedCount { get; set; }
        public string? Key { get; set; }
        
        // Navigation Properties
        public virtual Role Role { get; set; } = null!;
        
        // Domain Methods
        public void LockAccount(int minutes = 30)
        {
            LockoutEnd = DateTimeOffset.UtcNow.AddMinutes(minutes);
        }
        
        public bool IsLockedOut()
        {
            return LockoutEnd.HasValue && LockoutEnd.Value > DateTimeOffset.UtcNow;
        }
        
        public void ResetFailedAccessCount()
        {
            AccessFailedCount = 0;
        }
        
        public void IncrementAccessFailedCount()
        {
            AccessFailedCount++;
            if (AccessFailedCount >= 5)
            {
                LockAccount();
            }
        }
    }
}

// Domain Entity: Role
namespace AuthService.Domain.WriteModels
{
    public partial class Role
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public string NormalizedName { get; set; } = null!;
        
        // Navigation Properties
        public virtual ICollection<Account> Users { get; set; } = new List<Account>();
        
        // Domain Methods
        public bool IsSystemRole()
        {
            return Name.Equals("Admin", StringComparison.OrdinalIgnoreCase) ||
                   Name.Equals("System", StringComparison.OrdinalIgnoreCase);
        }
    }
}
```

##### 1.1.2 CQRS Commands and Queries
```csharp
// Commands
namespace AuthService.Application.Commands
{
    public record CreateAccountCommand(
        string Email,
        string Password,
        Guid RoleId,
        string CreatedBy
    ) : ICommand<CreateAccountResponse>;
    
    public record UpdateAccountCommand(
        Guid AccountId,
        string? Email,
        bool? IsActive,
        string UpdatedBy
    ) : ICommand<UpdateAccountResponse>;
    
    public record LoginCommand(
        string Email,
        string Password,
        string IpAddress
    ) : ICommand<LoginResponse>;
}

// Queries
namespace AuthService.Application.Queries
{
    public record GetAccountByIdQuery(Guid AccountId) : IQuery<AccountDto>;
    
    public record GetAccountByEmailQuery(string Email) : IQuery<AccountDto>;
    
    public record GetAccountsQuery(
        int PageNumber = 1,
        int PageSize = 10,
        string? SearchTerm = null,
        bool? IsActive = null
    ) : IQuery<PagedResult<AccountDto>>;
}

// Command Handlers
namespace AuthService.Application.Handlers.Commands
{
    public class CreateAccountCommandHandler : ICommandHandler<CreateAccountCommand, CreateAccountResponse>
    {
        private readonly IAccountRepository _accountRepository;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IEmailService _emailService;
        private readonly IUnitOfWork _unitOfWork;
        
        public CreateAccountCommandHandler(
            IAccountRepository accountRepository,
            IPasswordHasher passwordHasher,
            IEmailService emailService,
            IUnitOfWork unitOfWork)
        {
            _accountRepository = accountRepository;
            _passwordHasher = passwordHasher;
            _emailService = emailService;
            _unitOfWork = unitOfWork;
        }
        
        public async Task<CreateAccountResponse> Handle(CreateAccountCommand request, CancellationToken cancellationToken)
        {
            // Validation
            var existingAccount = await _accountRepository.GetByEmailAsync(request.Email);
            if (existingAccount != null)
                throw new DuplicateEmailException($"Account with email {request.Email} already exists");
            
            // Create new account
            var account = new Account
            {
                AccountId = Guid.NewGuid(),
                Email = request.Email,
                PasswordHash = _passwordHasher.HashPassword(request.Password),
                RoleId = request.RoleId,
                CreatedBy = request.CreatedBy,
                UpdatedBy = request.CreatedBy,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Key = GenerateVerificationKey()
            };
            
            await _accountRepository.AddAsync(account);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            
            // Send verification email
            await _emailService.SendVerificationEmailAsync(account.Email, account.Key);
            
            return new CreateAccountResponse(account.AccountId, "Account created successfully");
        }
        
        private string GenerateVerificationKey()
        {
            return Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        }
    }
}
```

#### 1.2 Infrastructure Layer Design

##### 1.2.1 Repository Pattern Implementation
```csharp
// Generic Repository Interface
namespace Shared.Infrastructure.Repositories
{
    public interface IGenericRepository<T> where T : class
    {
        Task<T?> GetByIdAsync(object id);
        Task<IEnumerable<T>> GetAllAsync();
        Task<T> AddAsync(T entity);
        Task UpdateAsync(T entity);
        Task DeleteAsync(T entity);
        Task<bool> ExistsAsync(object id);
    }
}

// Account Repository Interface
namespace AuthService.Infrastructure.Repositories
{
    public interface IAccountRepository : IGenericRepository<Account>
    {
        Task<Account?> GetByEmailAsync(string email);
        Task<Account?> GetByVerificationKeyAsync(string key);
        Task<PagedResult<Account>> GetPagedAsync(int pageNumber, int pageSize, string? searchTerm, bool? isActive);
        Task<bool> IsEmailUniqueAsync(string email, Guid? excludeAccountId = null);
    }
    
    // Implementation
    public class AccountRepository : GenericRepository<Account>, IAccountRepository
    {
        private readonly AuthServiceContext _context;
        
        public AccountRepository(AuthServiceContext context) : base(context)
        {
            _context = context;
        }
        
        public async Task<Account?> GetByEmailAsync(string email)
        {
            return await _context.Accounts
                .Include(a => a.Role)
                .FirstOrDefaultAsync(a => a.Email == email);
        }
        
        public async Task<Account?> GetByVerificationKeyAsync(string key)
        {
            return await _context.Accounts
                .Include(a => a.Role)
                .FirstOrDefaultAsync(a => a.Key == key);
        }
        
        public async Task<PagedResult<Account>> GetPagedAsync(int pageNumber, int pageSize, string? searchTerm, bool? isActive)
        {
            var query = _context.Accounts.Include(a => a.Role).AsQueryable();
            
            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(a => a.Email.Contains(searchTerm));
            }
            
            if (isActive.HasValue)
            {
                query = query.Where(a => a.IsActive == isActive.Value);
            }
            
            var totalCount = await query.CountAsync();
            var items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            
            return new PagedResult<Account>(items, totalCount, pageNumber, pageSize);
        }
        
        public async Task<bool> IsEmailUniqueAsync(string email, Guid? excludeAccountId = null)
        {
            var query = _context.Accounts.Where(a => a.Email == email);
            
            if (excludeAccountId.HasValue)
            {
                query = query.Where(a => a.AccountId != excludeAccountId.Value);
            }
            
            return !await query.AnyAsync();
        }
    }
}
```

##### 1.2.2 Database Context Configuration
```csharp
namespace AuthService.Infrastructure.Context
{
    public class AuthServiceContext : AppDbContext
    {
        public AuthServiceContext(DbContextOptions<AuthServiceContext> options) : base(options) { }
        
        public virtual DbSet<Account> Accounts { get; set; }
        public virtual DbSet<Role> Roles { get; set; }
        
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            builder.UseOpenIddict();
            
            // Account Entity Configuration
            builder.Entity<Account>(entity =>
            {
                entity.HasKey(x => x.AccountId);
                entity.ToTable("Accounts");
                
                // Properties
                entity.Property(x => x.Email)
                    .IsRequired()
                    .HasMaxLength(256);
                    
                entity.Property(x => x.PasswordHash)
                    .HasMaxLength(512);
                    
                entity.Property(x => x.CreatedBy)
                    .IsRequired()
                    .HasMaxLength(256);
                    
                entity.Property(x => x.UpdatedBy)
                    .IsRequired()
                    .HasMaxLength(256);
                    
                entity.Property(x => x.Key)
                    .HasMaxLength(256);
                
                // Default Values
                entity.Property(x => x.CreatedAt)
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");
                    
                entity.Property(x => x.UpdatedAt)
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");
                    
                entity.Property(x => x.IsActive)
                    .HasDefaultValue(true);
                    
                entity.Property(x => x.AccessFailedCount)
                    .HasDefaultValue(0);
                    
                entity.Property(x => x.EmailConfirmed)
                    .HasDefaultValue(false);
                
                // Indexes
                entity.HasIndex(x => x.Email)
                    .IsUnique()
                    .HasDatabaseName("IX_Accounts_Email");
                    
                entity.HasIndex(x => x.RoleId)
                    .HasDatabaseName("IX_Accounts_RoleId");
                    
                entity.HasIndex(x => x.IsActive)
                    .HasDatabaseName("IX_Accounts_IsActive");
                
                // Relationships
                entity.HasOne(u => u.Role)
                    .WithMany(r => r.Users)
                    .HasForeignKey(u => u.RoleId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
            
            // Role Entity Configuration
            builder.Entity<Role>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.ToTable("Roles");
                
                entity.Property(x => x.Name)
                    .IsRequired()
                    .HasMaxLength(256);
                    
                entity.Property(x => x.NormalizedName)
                    .IsRequired()
                    .HasMaxLength(256);
                
                entity.HasIndex(x => x.NormalizedName)
                    .IsUnique()
                    .HasDatabaseName("IX_Roles_NormalizedName");
            });
            
            // Seed Data
            SeedDefaultRoles(builder);
        }
        
        private void SeedDefaultRoles(ModelBuilder builder)
        {
            var roles = new[]
            {
                new Role { Id = Guid.Parse("11111111-1111-1111-1111-111111111111"), Name = "Admin", NormalizedName = "ADMIN" },
                new Role { Id = Guid.Parse("22222222-2222-2222-2222-222222222222"), Name = "Teacher", NormalizedName = "TEACHER" },
                new Role { Id = Guid.Parse("33333333-3333-3333-3333-333333333333"), Name = "Student", NormalizedName = "STUDENT" }
            };
            
            builder.Entity<Role>().HasData(roles);
        }
    }
}
```

### 2. Service Layer Design

#### 2.1 Authentication Service Design
```csharp
namespace AuthService.Application.Services
{
    public interface IAuthenticationService
    {
        Task<LoginResponse> AuthenticateAsync(string email, string password, string ipAddress);
        Task<RefreshTokenResponse> RefreshTokenAsync(string refreshToken);
        Task<bool> ValidateTokenAsync(string token);
        Task LogoutAsync(string refreshToken);
        Task<bool> ResetPasswordAsync(string email);
        Task<bool> ConfirmEmailAsync(string email, string token);
    }
    
    public class AuthenticationService : IAuthenticationService
    {
        private readonly IAccountRepository _accountRepository;
        private readonly IJwtTokenService _jwtTokenService;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly IEmailService _emailService;
        private readonly ILogger<AuthenticationService> _logger;
        
        public AuthenticationService(
            IAccountRepository accountRepository,
            IJwtTokenService jwtTokenService,
            IPasswordHasher passwordHasher,
            IRefreshTokenRepository refreshTokenRepository,
            IEmailService emailService,
            ILogger<AuthenticationService> logger)
        {
            _accountRepository = accountRepository;
            _jwtTokenService = jwtTokenService;
            _passwordHasher = passwordHasher;
            _refreshTokenRepository = refreshTokenRepository;
            _emailService = emailService;
            _logger = logger;
        }
        
        public async Task<LoginResponse> AuthenticateAsync(string email, string password, string ipAddress)
        {
            var account = await _accountRepository.GetByEmailAsync(email);
            
            if (account == null)
            {
                _logger.LogWarning("Login attempt failed: Account not found for email {Email}", email);
                throw new UnauthorizedAccessException("Invalid email or password");
            }
            
            if (account.IsLockedOut())
            {
                _logger.LogWarning("Login attempt failed: Account locked for email {Email}", email);
                throw new AccountLockedException("Account is temporarily locked due to multiple failed login attempts");
            }
            
            if (!account.IsActive)
            {
                _logger.LogWarning("Login attempt failed: Account inactive for email {Email}", email);
                throw new UnauthorizedAccessException("Account is not active");
            }
            
            if (!_passwordHasher.VerifyPassword(password, account.PasswordHash))
            {
                account.IncrementAccessFailedCount();
                await _accountRepository.UpdateAsync(account);
                
                _logger.LogWarning("Login attempt failed: Invalid password for email {Email}", email);
                throw new UnauthorizedAccessException("Invalid email or password");
            }
            
            // Reset failed access count on successful login
            account.ResetFailedAccessCount();
            await _accountRepository.UpdateAsync(account);
            
            // Generate tokens
            var accessToken = _jwtTokenService.GenerateAccessToken(account);
            var refreshToken = await _jwtTokenService.GenerateRefreshTokenAsync(account.AccountId, ipAddress);
            
            _logger.LogInformation("Successful login for email {Email}", email);
            
            return new LoginResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresIn = 900, // 15 minutes
                TokenType = "Bearer",
                User = new UserDto
                {
                    Id = account.AccountId,
                    Email = account.Email,
                    Role = account.Role.Name,
                    EmailConfirmed = account.EmailConfirmed
                }
            };
        }
    }
}
```

### 3. AI Integration Design

#### 3.1 AI Service Interfaces
```csharp
namespace Shared.Application.Interfaces.AI
{
    public interface IComputerVisionService
    {
        Task<ObjectDetectionResult> DetectObjectsAsync(byte[] imageData, string contentType);
        Task<ImageClassificationResult> ClassifyImageAsync(byte[] imageData, string contentType);
        Task<TextExtractionResult> ExtractTextAsync(byte[] imageData, string contentType);
    }
    
    public interface ISpeechService
    {
        Task<SpeechToTextResult> ConvertSpeechToTextAsync(byte[] audioData, string contentType);
        Task<TextToSpeechResult> ConvertTextToSpeechAsync(string text, string language = "en-US");
        Task<VoiceAnalysisResult> AnalyzeVoiceAsync(byte[] audioData, string contentType);
    }
    
    // Implementation for Ultralytics HUB
    public class UltralyticsComputerVisionService : IComputerVisionService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<UltralyticsComputerVisionService> _logger;
        
        public UltralyticsComputerVisionService(
            HttpClient httpClient,
            IConfiguration configuration,
            ILogger<UltralyticsComputerVisionService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
        }
        
        public async Task<ObjectDetectionResult> DetectObjectsAsync(byte[] imageData, string contentType)
        {
            try
            {
                var apiKey = _configuration["Ultralytics:ApiKey"];
                var endpoint = _configuration["Ultralytics:DetectionEndpoint"];
                
                using var content = new MultipartFormDataContent();
                using var imageContent = new ByteArrayContent(imageData);
                imageContent.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);
                
                content.Add(imageContent, "image", "image.jpg");
                
                _httpClient.DefaultRequestHeaders.Authorization = 
                    new AuthenticationHeaderValue("Bearer", apiKey);
                
                var response = await _httpClient.PostAsync(endpoint, content);
                response.EnsureSuccessStatusCode();
                
                var jsonResult = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<ObjectDetectionResult>(jsonResult);
                
                _logger.LogInformation("Object detection completed successfully");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during object detection");
                throw new AIServiceException("Failed to detect objects in image", ex);
            }
        }
    }
}
```
