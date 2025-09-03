using AuthService.Domain.ReadModels;
using AuthService.Infrastructure.Context;
using JasperFx;
using Marten;
using Microsoft.EntityFrameworkCore;
using Shared.Common.Utils.Const;
using Shared.Infrastructure.Contexts;
using StackExchange.Redis;

namespace AuthService.API.Extensions;

public static class DatabaseExtensions
{
    public static IServiceCollection AddDatabaseServices(this IServiceCollection services)
    {
        var connectionString = Environment.GetEnvironmentVariable(ConstEnv.AuthServiceDb);
        
        // C#
        services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect("localhost:6379,password=Gi@khoi221203,allowAdmin=true"));        
        services.AddScoped<IDatabase>(sp => sp.GetRequiredService<IConnectionMultiplexer>().GetDatabase());
        
        // Entity Framework configuration
        services.AddDbContext<AuthServiceContext>(options =>
        {
            options.UseNpgsql(connectionString);
            options.UseOpenIddict();
        });
        
        services.AddScoped<AppDbContext, AuthServiceContext>();
        
        // Marten document database configuration
        services.AddMarten(options =>
        {
            options.Connection(connectionString!);
            options.AutoCreateSchemaObjects = AutoCreate.All;
            options.DatabaseSchemaName = "AuthServiceDB_Marten";
            
            options.Schema.For<AccountCollection>().Identity(x => x.AccountId);
            options.Schema.For<RoleCollection>().Identity(x => x.Id);
        });
        
        return services;
    }
    
    public static async Task<WebApplication> EnsureDatabaseCreatedAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AuthServiceContext>();
        await db.Database.EnsureCreatedAsync();
        return app;
    }
}