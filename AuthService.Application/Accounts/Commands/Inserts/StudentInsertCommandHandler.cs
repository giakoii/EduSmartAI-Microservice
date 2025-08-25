using AuthService.Domain.ReadModels;
using AuthService.Domain.Snapshort;
using AuthService.Domain.WriteModels;
using BuildingBlocks.CQRS;
using BuildingBlocks.Messaging.Events.InsertUserEvents;
using MassTransit;
using Shared.Application.Interfaces.Commons;
using Shared.Application.Interfaces.Repositories;
using Shared.Application.Utils.Const;

namespace AuthService.Application.Accounts.Commands.Inserts;

public class StudentInsertCommandHandler : ICommandHandler<StudentInsertCommand, StudentInsertResponse>
{
    private readonly ICommandRepository<Account> _accountCommandRepository;
    private readonly ICommandRepository<Role> _roleCommandRepository;
    private readonly IQueryRepository<AccountCollection> _accountQueryRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IRequestClient<UserInsertEvent> _requestClient;
    private readonly ICommonLogic _commonLogic;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="accountCommandRepository"></param>
    /// <param name="unitOfWork"></param>
    /// <param name="requestClient"></param>
    /// <param name="commonLogic"></param>
    /// <param name="roleCommandRepository"></param>
    /// <param name="accountQueryRepository"></param>
    public StudentInsertCommandHandler(ICommandRepository<Account> accountCommandRepository, IUnitOfWork unitOfWork, 
        IRequestClient<UserInsertEvent> requestClient, IQueryRepository<AccountCollection> accountQueryRepository,
        ICommonLogic commonLogic, ICommandRepository<Role> roleCommandRepository)
    {
        _accountCommandRepository = accountCommandRepository;
        _unitOfWork = unitOfWork;
        _requestClient = requestClient;
        _accountQueryRepository = accountQueryRepository;
        _commonLogic = commonLogic;
        _roleCommandRepository = roleCommandRepository;
    }

    /// <summary>
    /// Handles the insertion of a new account.
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<StudentInsertResponse> Handle(StudentInsertCommand request, CancellationToken cancellationToken)
    {
        var response = new StudentInsertResponse { Success = false };
        
        // Check role existence
        var role = await _roleCommandRepository.FirstOrDefaultAsync(x => x.Name == nameof(ConstantEnum.UserRole.Student), cancellationToken);
        if (role == null)
        {
            response.SetMessage(MessageId.E99999);
            return response;
        }
        
        // Check account existence
        var existingAccount = await _accountCommandRepository.FirstOrDefaultAsync(x => x.Email == request.Email && x.IsActive, cancellationToken);
        if (existingAccount != null)
        {
            var userCollectionExisting = await _accountQueryRepository.FirstOrDefaultAsync(x => x.Email == request.Email && x.IsActive);
            if (userCollectionExisting == null)
            {
                response.SetMessage(MessageId.E00000, "System error: Users not found.");
                return response;
            }
            
            // Check account isn't verified and not expired
            if (!existingAccount.EmailConfirmed && existingAccount.CreatedAt < DateTime.UtcNow.AddMinutes(-5))
            {
                // Delete the existing account and create a new one
                await _unitOfWork.BeginTransactionAsync(async () =>
                {
                    // Delete an existing account
                    _accountCommandRepository.Update(existingAccount);
                    await _unitOfWork.SaveChangesAsync(existingAccount.Email, cancellationToken, true);
                    
                    // Remove an existing account from the collection
                    _unitOfWork.Delete(AccountCollection.FromWriteModel(existingAccount, userCollectionExisting.UserInformation));
                    await _unitOfWork.SessionSaveChangesAsync();
                    
                    // Create a new account
                    var newUser = await InsertAccount(request, role.Id);
                    
                    // Publish event for account creation
                    var @event = new UserInsertEvent
                    {
                        UserId = newUser.AccountId,
                        OldUserId = existingAccount.AccountId,
                        FirstName = request.FirstName,
                        LastName = request.LastName,
                        UserRole = (byte) ConstantEnum.UserRole.Student,
                        Email = newUser.Email,
                    };
                    
                    // Send request client to UserService
                    var userInsertResponse = await _requestClient.GetResponse<UserInsertEventResponse>(@event, cancellationToken);
                    if (!userInsertResponse.Message.Success)
                    {
                        response.SetMessage(MessageId.E00000, userInsertResponse.Message.Message);
                        return false;
                    }
                    
                    response.Success = true;
                    response.SetMessage(MessageId.I00001);
                    return true;
                }, cancellationToken);
            }
        }
        
        await _unitOfWork.BeginTransactionAsync(async () =>
        {
            var newAccount = await InsertAccount(request, role.Id);
            
            // Publish event for account creation
            var @event = new UserInsertEvent
            {
                UserId = newAccount.AccountId,
                FirstName = request.FirstName,
                LastName = request.LastName,
                UserRole = (byte) ConstantEnum.UserRole.Student,
                Email = newAccount.Email,
            };
            
            // Send request client to UserService
            var userInsertResponse = await _requestClient.GetResponse<UserInsertEventResponse>(@event, cancellationToken);
            if (!userInsertResponse.Message.Success)
            {
                response.SetMessage(MessageId.E00000, userInsertResponse.Message.Message);
                return false;
            }

            // Create new user information
            var userInformation = new UserInformation
            {
                FirstName = request.FirstName,
                LastName = request.LastName,
            };
            
            // Store the new account in the collection
            _unitOfWork.Store(AccountCollection.FromWriteModel(newAccount,userInformation));
            await _unitOfWork.SessionSaveChangesAsync();
            
            // True
            response.Success = true;
            response.SetMessage(MessageId.I00001);
            return true;
        }, cancellationToken);
        return response;
    }

    /// <summary>
    /// Inserts a new account into the database.
    /// </summary>
    /// <param name="request"></param>
    /// <param name="roleId"></param>
    /// <returns></returns>
    private async Task<Account> InsertAccount(StudentInsertCommand request, Guid roleId)
    {
        var key = _commonLogic.EncryptText(request.Email);
        
        var newAccount = new Account
        {
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password, workFactor: 12),
            EmailConfirmed = false,
            Key = key,
            RoleId = roleId,
        };
        
        // Add a new account to the database
        await _accountCommandRepository.AddAsync(newAccount);
        await _unitOfWork.SaveChangesAsync(newAccount.Email);
        
        return newAccount;
    }
}