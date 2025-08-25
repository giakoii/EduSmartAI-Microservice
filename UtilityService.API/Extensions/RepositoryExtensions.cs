using Shared.Application.Interfaces.Commons;
using Shared.Application.Interfaces.IdentityHepers;
using Shared.Application.Interfaces.Repositories;
using Shared.Infrastructure.Identities;
using Shared.Infrastructure.Logics;
using Shared.Infrastructure.Repositories;
using UtilityService.Application.Interfaces;
using UtilityService.Domain.Models;
using UtilityService.Infrastructure.Implements;

namespace UtilityService.API.Extensions;

public static class RepositoryExtensions
{
    public static IServiceCollection AddRepositoryServices(this IServiceCollection services)
    {
        // Repository services
        services.AddScoped<ICommonLogic, CommonLogic>();
        services.AddScoped<IIdentityService, IdentityService>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<ICommandRepository<CloudinaryConfig>, CommandRepository<CloudinaryConfig>>();
        services.AddScoped<ICloudinaryService, CloudinaryService>();

        return services;
    }
}