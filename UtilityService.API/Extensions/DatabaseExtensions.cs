using JasperFx;
using Marten;
using Microsoft.EntityFrameworkCore;
using Shared.Common.Utils.Const;
using Shared.Infrastructure.Contexts;
using UtilityService.Infrastructure.Contexts;

namespace UtilityService.API.Extensions;

public static class DatabaseExtensions
{
    public static IServiceCollection AddDatabaseServices(this IServiceCollection services)
    {
        var connectionString = Environment.GetEnvironmentVariable(ConstEnv.UtilityServiceDb);
        
        // Entity Framework configuration
        services.AddDbContext<UtilityServiceContext>(options =>
        {
            options.UseNpgsql(connectionString);
        });
        
        services.AddScoped<AppDbContext, UtilityServiceContext>();
        services.AddMarten(options =>
        {
            options.Connection(connectionString!);
        });
        return services;
    }
    
    public static async Task<WebApplication> EnsureDatabaseCreatedAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<UtilityServiceContext>();
        await db.Database.EnsureCreatedAsync();
        return app;
    }
}