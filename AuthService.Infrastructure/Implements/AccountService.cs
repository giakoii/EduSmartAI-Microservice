using AuthService.Application.Accounts.Commands.Inserts;
using AuthService.Application.Interfaces;
using AuthService.Domain.ReadModels;
using AuthService.Domain.Snapshort;
using AuthService.Domain.WriteModels;
using BuildingBlocks.Messaging.Events.InsertUserEvents;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Shared.Application.Interfaces.Commons;
using Shared.Application.Interfaces.Repositories;
using Shared.Application.Utils.Const;

namespace AuthService.Infrastructure.Implements;

public class AccountService : IAccountService
{
    private readonly ICommandRepository<Account> _accountCommandRepository;
    private readonly ICommandRepository<Role> _roleCommandRepository;
    private readonly IQueryRepository<AccountCollection> _accountQueryRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IRequestClient<UserInsertEvent> _requestClient;
    private readonly ICommonLogic _commonLogic;

    public AccountService(
        ICommandRepository<Account> accountCommandRepository,
        ICommandRepository<Role> roleCommandRepository,
        IQueryRepository<AccountCollection> accountQueryRepository,
        IUnitOfWork unitOfWork,
        IRequestClient<UserInsertEvent> requestClient,
        ICommonLogic commonLogic)
    {
        _accountCommandRepository = accountCommandRepository;
        _roleCommandRepository = roleCommandRepository;
        _accountQueryRepository = accountQueryRepository;
        _unitOfWork = unitOfWork;
        _requestClient = requestClient;
        _commonLogic = commonLogic;
    }
    public async Task<StudentInsertResponse> InsertStudentAsync(StudentInsertCommand request, CancellationToken cancellationToken)
    {
        var response = new StudentInsertResponse { Success = false };

        // Check role existence
        var role = await _roleCommandRepository.FirstOrDefaultAsync(x => x.Name == nameof(ConstantEnum.UserRole.Student), cancellationToken);
        if (role == null)
        {
            response.SetMessage(MessageId.E99999);
            return response;
        }

        // Check the existing account
        var existingAccount = await _accountCommandRepository.FirstOrDefaultAsync(
            x => x.Email == request.Email && x.IsActive,
            cancellationToken);

        if (existingAccount != null)
        {
            var userCollectionExisting = await _accountQueryRepository.FirstOrDefaultAsync(
                x => x.Email == request.Email && x.IsActive);

            if (userCollectionExisting == null)
            {
                response.SetMessage(MessageId.E11001);
                return response;
            }

            // Delete and recreate the account if email is not confirmed and created more than 5 minutes ago
            if (!existingAccount.EmailConfirmed && existingAccount.CreatedAt < DateTime.UtcNow.AddMinutes(-5))
            {
                await _unitOfWork.BeginTransactionAsync(async () =>
                {
                    // Delete the existing account
                    _accountCommandRepository.Update(existingAccount);
                    await _unitOfWork.SaveChangesAsync(existingAccount.Email, cancellationToken, true);

                    _unitOfWork.Delete(AccountCollection.FromWriteModel(existingAccount, userCollectionExisting.UserInformation));
                    await _unitOfWork.SessionSaveChangesAsync();

                    // Insert the new account
                    var newUser = await InsertAccountAsync(request, role.Id);

                    // Send event to create new user
                    var @event = new UserInsertEvent
                    {
                        UserId = newUser.AccountId,
                        OldUserId = existingAccount.AccountId,
                        FirstName = request.FirstName,
                        LastName = request.LastName,
                        UserRole = (byte)ConstantEnum.UserRole.Student,
                        Email = newUser.Email,
                    };

                    // Publish event for account creation
                    var userInsertResponse = await _requestClient.GetResponse<UserInsertEventResponse>(@event, cancellationToken);
                    if (!userInsertResponse.Message.Success)
                    {
                        response.SetMessage(MessageId.E00000, userInsertResponse.Message.Message);
                        return false;
                    }

                    // True
                    response.Success = true;
                    response.SetMessage(MessageId.I00001);
                    return true;
                }, cancellationToken);
                return response;
            }
        }

        // === INSERT NEW ACCOUNT ===
        await _unitOfWork.BeginTransactionAsync(async () =>
        {
            var newAccount = await InsertAccountAsync(request, role.Id);

            var @event = new UserInsertEvent
            {
                UserId = newAccount.AccountId,
                FirstName = request.FirstName,
                LastName = request.LastName,
                UserRole = (byte)ConstantEnum.UserRole.Student,
                Email = newAccount.Email,
            };

            var userInsertResponse = await _requestClient.GetResponse<UserInsertEventResponse>(@event, cancellationToken);
            if (!userInsertResponse.Message.Success)
            {
                response.SetMessage(MessageId.E00000, userInsertResponse.Message.Message);
                return false;
            }

            var userInformation = new UserInformation
            {
                FirstName = request.FirstName,
                LastName = request.LastName,
            };

            _unitOfWork.Store(AccountCollection.FromWriteModel(newAccount, userInformation));
            await _unitOfWork.SessionSaveChangesAsync();

            response.Success = true;
            response.SetMessage(MessageId.I00001);
            return true;
        }, cancellationToken);

        return response;
    }

    /// <summary>
    /// Validates user by email,
    /// checks if account exists,
    /// is active, email confirmed, and not locked.
    /// </summary>
    /// <param name="email"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<(bool Success, string MessageId, Account? Account)> ValidateUserAsync(string email, CancellationToken cancellationToken)
    {
        // Check if the account exists, is active, and email is confirmed
        var account = await _accountCommandRepository.FirstOrDefaultAsync(x => x.Email == email && x.IsActive && x.EmailConfirmed, cancellationToken);
        
        // The Account does not exist, or is not active or email not confirmed
        if (account == null) return (false, MessageId.E11005, null);

        // Check if the account is locked
        if (account.LockoutEnd.HasValue && account.LockoutEnd > DateTimeOffset.Now) return (false, MessageId.E11003, null);
        
        return (true, string.Empty , account);
    }

    /// <summary>
    /// Check if the provided password matches the stored password for the given account.
    /// </summary>
    /// <param name="account"></param>
    /// <param name="password"></param>
    /// <returns></returns>
    public bool CheckPassword(Account account, string password)
    {
        return BCrypt.Net.BCrypt.Verify(password, account.PasswordHash);
    }

    /// <summary>
    /// Locks the account after a certain number of failed login attempts.
    /// </summary>
    /// <param name="account"></param>
    public void LockAccount(Account account)
    {
        account.AccessFailedCount++;
        if (account.AccessFailedCount >= 5)
            account.LockoutEnd = DateTimeOffset.Now + TimeSpan.FromMinutes(5);

        _accountCommandRepository.Update(account);
    }

    /// <summary>
    /// Resets the failed login attempts counter after a successful login.
    /// </summary>
    /// <param name="account"></param>
    public void ResetFailedAttempts(Account account)
    {
        account.AccessFailedCount = 0;
        account.LockoutEnd = null;
        _accountCommandRepository.Update(account);
    }

    /// <summary>
    /// Retrieves the role details based on the role name.
    /// </summary>
    /// <param name="roleId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<string?> GetUserRoleNameAsync(Guid roleId, CancellationToken cancellationToken)
    {
        return await _roleCommandRepository
            .Find(x => x.Id == roleId)
            .Select(x => x!.Name)
            .FirstOrDefaultAsync(cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Inserts a new account into the database.
    /// </summary>
    /// <param name="request"></param>
    /// <param name="roleId"></param>
    /// <returns></returns>
    private async Task<Account> InsertAccountAsync(StudentInsertCommand request, Guid roleId)
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

        await _accountCommandRepository.AddAsync(newAccount);
        await _unitOfWork.SaveChangesAsync(newAccount.Email);

        return newAccount;
    }
}