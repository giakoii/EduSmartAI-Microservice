using AuthService.Domain.WriteModels;
using BuildingBlocks.CQRS;
using BuildingBlocks.Messaging.Events.UserLoginEvents;
using MassTransit;
using Shared.Application.Interfaces.Repositories;
using Shared.Application.Utils.Const;

namespace AuthService.Application.Accounts.Commands.Logins;

public class UserLoginCommandHandler : ICommandHandler<UserLoginCommand, UserLoginResponse>
{
    private readonly ICommandRepository<Account> _accountRepository;
    private readonly ICommandRepository<Role> _roleRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IRequestClient<UserLoginEvent> _requestClient;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="accountRepository"></param>
    /// <param name="roleRepository"></param>
    /// <param name="unitOfWork"></param>
    /// <param name="requestClient"></param>
    public UserLoginCommandHandler(ICommandRepository<Account> accountRepository, ICommandRepository<Role> roleRepository,
        IUnitOfWork unitOfWork, IRequestClient<UserLoginEvent> requestClient)
    {
        _accountRepository = accountRepository;
        _roleRepository = roleRepository;
        _unitOfWork = unitOfWork;
        _requestClient = requestClient;
    }

    public async Task<UserLoginResponse> Handle(UserLoginCommand request, CancellationToken cancellationToken)
    {
        var response = new UserLoginResponse { Success = false };

        // Validate user credentials
        var account = await _accountRepository.FirstOrDefaultAsync(x => x.Email == request.UserName && x.IsActive && x.EmailConfirmed, cancellationToken);
        if (account == null)
        {
            response.SetMessage(MessageId.E11001);
            return response;
        }
        
        // Check if the account is locked
        if (account.LockoutEnd.HasValue && account.LockoutEnd > DateTimeOffset.Now)
        {
            response.SetMessage(MessageId.E11003);
            return response;
        }
        
        // Begin transaction
        await _unitOfWork.BeginTransactionAsync(async () =>
        {
            // Check password, increment-failed attempts if incorrect
            if (!BCrypt.Net.BCrypt.Verify(request.Password, account.PasswordHash))
            {
                account.AccessFailedCount++;
            
                if (account.AccessFailedCount >= 5)
                {
                    account.LockoutEnd = DateTimeOffset.Now + TimeSpan.FromMinutes(5);
                }
            
                _accountRepository.Update(account);
                await _unitOfWork.SaveChangesAsync(account.Email, cancellationToken);
            
                response.SetMessage(MessageId.E11002);
                return false;
            }

            // Check the role in the database
            var role = await _roleRepository.FirstOrDefaultAsync(x => x.Id == account.RoleId, cancellationToken);
            if (role == null)
            {
                response.SetMessage(MessageId.E99999);
                return false;
            }
        
            var @event = new UserLoginEvent
            {
                UserId = account.AccountId,
            };
        
            // Send event to insert user
            var userLoginMessageResponse = await _requestClient.GetResponse<UserLoginEventResponse>(@event, cancellationToken);
            if (!userLoginMessageResponse.Message.Success)
            {
                response.SetMessage(userLoginMessageResponse.Message.MessageId, userLoginMessageResponse.Message.Message);
                return false;
            }
        
            // Reset failed attempts on successful login
            account.AccessFailedCount = 0;
            account.LockoutEnd = null;
        
            _accountRepository.Update(account);
            await _unitOfWork.SaveChangesAsync(account.Email, cancellationToken);

            var responseMessage = userLoginMessageResponse.Message.Response;
            
            // Set successful response
            response.Response = new UserLoginEntity(
                UserId: account.AccountId,
                FullName: $"{responseMessage.FirstName} {responseMessage.LastName}",
                Email: account.Email,
                RoleName: role.Name
            );
        
            // True
            response.Success = true;
            response.SetMessage(MessageId.I00001);
            return true;
        }, cancellationToken);
        return response;
    }
}