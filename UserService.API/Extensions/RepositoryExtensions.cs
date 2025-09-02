using Shared.Application.Interfaces.Commons;
using Shared.Application.Interfaces.IdentityHepers;
using Shared.Application.Interfaces.Repositories;
using Shared.Infrastructure.Identities;
using Shared.Infrastructure.Logics;
using Shared.Infrastructure.Repositories;
using UserService.Application.Users.Commands.Inserts;
using UserService.Application.Users.Queries.Logins;
using UserService.Domain.ReadModels;
using UserService.Domain.WriteModels;

namespace UserService.API.Extensions;

public static class RepositoryExtensions
{
    public static IServiceCollection AddRepositoryServices(this IServiceCollection services)
    {
        // Repository services
        services.AddScoped<ICommonLogic, CommonLogic>();
        services.AddScoped<IIdentityService, IdentityService>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IQueryRepository<StudentCollection>, QueryRepository<StudentCollection>>();
        services.AddScoped<IQueryRepository<TeacherCollection>, QueryRepository<TeacherCollection>>();
        services.AddScoped<ICommandRepository<Student>, CommandRepository<Student>>();
        services.AddScoped<ICommandRepository<Teacher>, CommandRepository<Teacher>>();
        
        // MediatR configuration
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssemblyContaining<UserInsertCommandHandler>();
            cfg.RegisterServicesFromAssemblyContaining<UserLoginQueryHandler>();
        });        
        return services;
    }
}