using AuthService.Application.Accounts.Commands.Inserts;
using AuthService.Application.Interfaces;
using AuthService.Application.Interfaces.TokenServices;
using AuthService.Domain.ReadModels;
using AuthService.Domain.WriteModels;
using AuthService.Infrastructure.Implements;
using Shared.Application.Interfaces.Commons;
using Shared.Application.Interfaces.IdentityHepers;
using Shared.Application.Interfaces.Repositories;
using Shared.Infrastructure.Identities;
using Shared.Infrastructure.Logics;
using Shared.Infrastructure.Repositories;

namespace AuthService.API.Extensions;

public static class RepositoryExtensions
{
    public static IServiceCollection AddRepositoryServices(this IServiceCollection services)
    {
        // Repository services
        services.AddScoped<ICommonLogic, CommonLogic>();
        services.AddScoped<ICommandRepository<Account>, CommandRepository<Account>>();
        services.AddScoped<ICommandRepository<Role>, CommandRepository<Role>>();
        services.AddScoped<IQueryRepository<AccountCollection>, QueryRepository<AccountCollection>>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IIdentityService, IdentityService>();
        services.AddScoped<IAccountService, AccountService>();
        
        // MediatR configuration
        services.AddMediatR(cfg => 
            cfg.RegisterServicesFromAssemblyContaining<StudentInsertCommandHandler>());
        
        return services;
    }
}