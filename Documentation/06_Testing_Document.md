# Testing Document
## EduSmartAI Backend System

### 1. Test Strategy Overview

#### 1.1 Testing Pyramid
```
                    E2E Tests
                   /           \
              Integration Tests
             /                   \
           Unit Tests (Foundation)
```

#### 1.2 Test Coverage Goals
- **Unit Tests**: 90% code coverage
- **Integration Tests**: 80% API endpoint coverage
- **E2E Tests**: 100% critical user journey coverage

### 2. Unit Testing Framework

#### 2.1 Test Structure
```csharp
// Test Project: AuthService.Application.Tests
using Xunit;
using Moq;
using FluentAssertions;
using AuthService.Application.Commands;
using AuthService.Application.Handlers.Commands;

namespace AuthService.Application.Tests.Handlers
{
    public class CreateAccountCommandHandlerTests
    {
        private readonly Mock<IAccountRepository> _mockAccountRepository;
        private readonly Mock<IPasswordHasher> _mockPasswordHasher;
        private readonly Mock<IEmailService> _mockEmailService;
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly CreateAccountCommandHandler _handler;

        public CreateAccountCommandHandlerTests()
        {
            _mockAccountRepository = new Mock<IAccountRepository>();
            _mockPasswordHasher = new Mock<IPasswordHasher>();
            _mockEmailService = new Mock<IEmailService>();
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            
            _handler = new CreateAccountCommandHandler(
                _mockAccountRepository.Object,
                _mockPasswordHasher.Object,
                _mockEmailService.Object,
                _mockUnitOfWork.Object);
        }

        [Fact]
        public async Task Handle_ValidRequest_ShouldCreateAccount()
        {
            // Arrange
            var command = new CreateAccountCommand(
                "test@example.com",
                "StrongPassword123!",
                Guid.NewGuid(),
                "System"
            );

            _mockAccountRepository
                .Setup(x => x.GetByEmailAsync(command.Email))
                .ReturnsAsync((Account)null);

            _mockPasswordHasher
                .Setup(x => x.HashPassword(command.Password))
                .Returns("HashedPassword");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            
            _mockAccountRepository.Verify(x => x.AddAsync(It.IsAny<Account>()), Times.Once);
            _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
            _mockEmailService.Verify(x => x.SendVerificationEmailAsync(
                command.Email, 
                It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task Handle_DuplicateEmail_ShouldThrowException()
        {
            // Arrange
            var command = new CreateAccountCommand(
                "existing@example.com",
                "StrongPassword123!",
                Guid.NewGuid(),
                "System"
            );

            var existingAccount = new Account { Email = command.Email };
            
            _mockAccountRepository
                .Setup(x => x.GetByEmailAsync(command.Email))
                .ReturnsAsync(existingAccount);

            // Act & Assert
            await Assert.ThrowsAsync<DuplicateEmailException>(
                () => _handler.Handle(command, CancellationToken.None));

            _mockAccountRepository.Verify(x => x.AddAsync(It.IsAny<Account>()), Times.Never);
        }

        [Theory]
        [InlineData("")]
        [InlineData("invalid-email")]
        [InlineData("@domain.com")]
        public async Task Handle_InvalidEmail_ShouldThrowValidationException(string invalidEmail)
        {
            // Arrange
            var command = new CreateAccountCommand(
                invalidEmail,
                "StrongPassword123!",
                Guid.NewGuid(),
                "System"
            );

            // Act & Assert
            await Assert.ThrowsAsync<ValidationException>(
                () => _handler.Handle(command, CancellationToken.None));
        }
    }
}
```

#### 2.2 Domain Model Tests
```csharp
public class AccountTests
{
    [Fact]
    public void LockAccount_ShouldSetLockoutEnd()
    {
        // Arrange
        var account = new Account();
        var lockoutMinutes = 30;

        // Act
        account.LockAccount(lockoutMinutes);

        // Assert
        account.LockoutEnd.Should().BeCloseTo(
            DateTimeOffset.UtcNow.AddMinutes(lockoutMinutes), 
            TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void IsLockedOut_WhenLockoutEndInFuture_ShouldReturnTrue()
    {
        // Arrange
        var account = new Account
        {
            LockoutEnd = DateTimeOffset.UtcNow.AddMinutes(10)
        };

        // Act
        var result = account.IsLockedOut();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IncrementAccessFailedCount_When5thFailure_ShouldLockAccount()
    {
        // Arrange
        var account = new Account { AccessFailedCount = 4 };

        // Act
        account.IncrementAccessFailedCount();

        // Assert
        account.AccessFailedCount.Should().Be(5);
        account.LockoutEnd.Should().NotBeNull();
        account.IsLockedOut().Should().BeTrue();
    }
}
```

### 3. Integration Testing

#### 3.1 API Integration Tests
```csharp
// Test Project: AuthService.API.Tests
[Collection("Database")]
public class AuthControllerIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public AuthControllerIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Register_ValidData_ShouldReturnCreated()
    {
        // Arrange
        var request = new
        {
            Email = "newuser@test.com",
            Password = "StrongPassword123!",
            RoleId = "33333333-3333-3333-3333-333333333333" // Student role
        };

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/auth/register", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        
        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ApiResponse<CreateAccountResponse>>(responseContent);
        
        result.Success.Should().BeTrue();
        result.Data.AccountId.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Login_ValidCredentials_ShouldReturnTokens()
    {
        // Arrange - First create an account
        await SeedTestAccount();

        var loginRequest = new
        {
            Email = "test@example.com",
            Password = "TestPassword123!"
        };

        var json = JsonSerializer.Serialize(loginRequest);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/auth/login", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ApiResponse<LoginResponse>>(responseContent);
        
        result.Success.Should().BeTrue();
        result.Data.AccessToken.Should().NotBeNullOrEmpty();
        result.Data.RefreshToken.Should().NotBeNullOrEmpty();
        result.Data.User.Email.Should().Be("test@example.com");
    }

    [Fact]
    public async Task Login_InvalidCredentials_ShouldReturnUnauthorized()
    {
        // Arrange
        var loginRequest = new
        {
            Email = "nonexistent@example.com",
            Password = "WrongPassword"
        };

        var json = JsonSerializer.Serialize(loginRequest);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/auth/login", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private async Task SeedTestAccount()
    {
        var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AuthServiceContext>();
        
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        
        var account = new Account
        {
            AccountId = Guid.NewGuid(),
            Email = "test@example.com",
            PasswordHash = hasher.HashPassword("TestPassword123!"),
            RoleId = Guid.Parse("33333333-3333-3333-3333-333333333333"),
            EmailConfirmed = true,
            IsActive = true,
            CreatedBy = "System",
            UpdatedBy = "System",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.Accounts.Add(account);
        await context.SaveChangesAsync();
    }
}
```

#### 3.2 Database Integration Tests
```csharp
public class AccountRepositoryIntegrationTests : IClassFixture<DatabaseFixture>
{
    private readonly AuthServiceContext _context;
    private readonly AccountRepository _repository;

    public AccountRepositoryIntegrationTests(DatabaseFixture fixture)
    {
        _context = fixture.Context;
        _repository = new AccountRepository(_context);
    }

    [Fact]
    public async Task GetByEmailAsync_ExistingEmail_ShouldReturnAccount()
    {
        // Arrange
        var account = new Account
        {
            AccountId = Guid.NewGuid(),
            Email = "repository.test@example.com",
            RoleId = Guid.Parse("33333333-3333-3333-3333-333333333333"),
            CreatedBy = "Test",
            UpdatedBy = "Test"
        };

        _context.Accounts.Add(account);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByEmailAsync("repository.test@example.com");

        // Assert
        result.Should().NotBeNull();
        result.Email.Should().Be("repository.test@example.com");
        result.Role.Should().NotBeNull();
    }

    [Fact]
    public async Task IsEmailUniqueAsync_NewEmail_ShouldReturnTrue()
    {
        // Act
        var result = await _repository.IsEmailUniqueAsync("unique.email@example.com");

        // Assert
        result.Should().BeTrue();
    }
}
```

### 4. End-to-End Testing

#### 4.1 User Registration E2E Test
```csharp
[Collection("E2E")]
public class UserRegistrationE2ETests : IClassFixture<E2ETestFixture>
{
    private readonly E2ETestFixture _fixture;

    public UserRegistrationE2ETests(E2ETestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task CompleteUserRegistrationFlow_ShouldSucceed()
    {
        // Step 1: Register new user
        var registrationResponse = await _fixture.Client.PostAsync("/api/auth/register", 
            CreateJsonContent(new
            {
                Email = "e2e.test@example.com",
                Password = "E2EPassword123!",
                RoleId = "33333333-3333-3333-3333-333333333333"
            }));

        registrationResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        // Step 2: Verify email (simulate email verification)
        var verificationKey = await GetVerificationKeyFromDatabase("e2e.test@example.com");
        var verificationResponse = await _fixture.Client.GetAsync($"/api/auth/verify-email/{verificationKey}");
        
        verificationResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Step 3: Login with verified account
        var loginResponse = await _fixture.Client.PostAsync("/api/auth/login",
            CreateJsonContent(new
            {
                Email = "e2e.test@example.com",
                Password = "E2EPassword123!"
            }));

        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Step 4: Access protected endpoint with token
        var loginContent = await loginResponse.Content.ReadAsStringAsync();
        var loginResult = JsonSerializer.Deserialize<ApiResponse<LoginResponse>>(loginContent);

        _fixture.Client.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Bearer", loginResult.Data.AccessToken);

        var profileResponse = await _fixture.Client.GetAsync("/api/auth/profile");
        profileResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private async Task<string> GetVerificationKeyFromDatabase(string email)
    {
        using var scope = _fixture.ServiceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AuthServiceContext>();
        
        var account = await context.Accounts.FirstOrDefaultAsync(a => a.Email == email);
        return account?.Key ?? throw new InvalidOperationException("Account not found");
    }

    private static StringContent CreateJsonContent(object data)
    {
        var json = JsonSerializer.Serialize(data);
        return new StringContent(json, Encoding.UTF8, "application/json");
    }
}
```

### 5. Performance Testing

#### 5.1 Load Testing Configuration
```csharp
// Using NBomber for load testing
public class AuthServiceLoadTests
{
    [Fact]
    public void LoginEndpoint_LoadTest()
    {
        var scenario = Scenario.Create("login_scenario", async context =>
        {
            var httpClient = new HttpClient();
            
            var loginData = new
            {
                Email = $"user{context.ScenarioInfo.ThreadId}@test.com",
                Password = "LoadTestPassword123!"
            };

            var json = JsonSerializer.Serialize(loginData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync("https://localhost:5001/api/auth/login", content);

            return response.IsSuccessStatusCode ? Response.Ok() : Response.Fail();
        })
        .WithLoadSimulations(
            Simulation.InjectPerSec(rate: 100, during: TimeSpan.FromMinutes(5)),
            Simulation.KeepConstant(copies: 50, during: TimeSpan.FromMinutes(3))
        );

        var stats = NBomberRunner
            .RegisterScenarios(scenario)
            .Run();

        // Assert performance requirements
        var loginScenarioStats = stats.AllOkCount;
        var errorRate = (double)stats.AllFailCount / stats.AllRequestCount;
        
        loginScenarioStats.Should().BeGreaterThan(0);
        errorRate.Should().BeLessThan(0.01); // Less than 1% error rate
    }
}
```

### 6. Test Data Management

#### 6.1 Test Database Setup
```csharp
public class DatabaseFixture : IDisposable
{
    public AuthServiceContext Context { get; private set; }

    public DatabaseFixture()
    {
        var options = new DbContextOptionsBuilder<AuthServiceContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        Context = new AuthServiceContext(options);
        
        // Seed test data
        SeedTestData();
    }

    private void SeedTestData()
    {
        var roles = new[]
        {
            new Role { Id = Guid.Parse("11111111-1111-1111-1111-111111111111"), Name = "Admin", NormalizedName = "ADMIN" },
            new Role { Id = Guid.Parse("22222222-2222-2222-2222-222222222222"), Name = "Teacher", NormalizedName = "TEACHER" },
            new Role { Id = Guid.Parse("33333333-3333-3333-3333-333333333333"), Name = "Student", NormalizedName = "STUDENT" }
        };

        Context.Roles.AddRange(roles);
        Context.SaveChanges();
    }

    public void Dispose()
    {
        Context?.Dispose();
    }
}
```

### 7. Test Execution Strategy

#### 7.1 Continuous Integration Pipeline
```yaml
# .github/workflows/tests.yml
name: Run Tests

on: [push, pull_request]

jobs:
  test:
    runs-on: ubuntu-latest
    
    services:
      postgres:
        image: postgres:13
        env:
          POSTGRES_PASSWORD: postgres
        options: >-
          --health-cmd pg_isready
          --health-interval 10s
          --health-timeout 5s
          --health-retries 5
          
      redis:
        image: redis:7-alpine
        options: >-
          --health-cmd "redis-cli ping"
          --health-interval 10s
          --health-timeout 5s
          --health-retries 5

    steps:
    - uses: actions/checkout@v3
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: 8.0.x
        
    - name: Restore dependencies
      run: dotnet restore
      
    - name: Build
      run: dotnet build --no-restore
      
    - name: Unit Tests
      run: dotnet test --no-build --verbosity normal --logger trx --collect:"XPlat Code Coverage"
      
    - name: Integration Tests
      run: dotnet test --no-build --verbosity normal --filter Category=Integration
      env:
        ConnectionStrings__DefaultConnection: Host=localhost;Database=edusmart_test;Username=postgres;Password=postgres
        ConnectionStrings__RedisConnection: localhost:6379
        
    - name: Generate Coverage Report
      run: |
        dotnet tool install -g dotnet-reportgenerator-globaltool
        reportgenerator -reports:**/coverage.cobertura.xml -targetdir:coverage -reporttypes:HtmlInline_AzurePipelines
        
    - name: Upload Coverage
      uses: codecov/codecov-action@v3
      with:
        files: coverage/Cobertura.xml
```

### 8. Test Results and Metrics

#### 8.1 Success Criteria
- **Unit Test Coverage**: Minimum 90%
- **Integration Test Coverage**: Minimum 80%
- **Performance Requirements**: 
  - API response time < 200ms (95th percentile)
  - Support 1000+ concurrent users
  - Error rate < 0.1%
- **Security Tests**: Pass OWASP security validation
- **Compatibility**: Support .NET 8.0+ environments
