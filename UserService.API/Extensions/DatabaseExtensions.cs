using BuildingBlocks.Messaging.Settings;
using JasperFx;
using Marten;
using Microsoft.EntityFrameworkCore;
using Shared.Common.Utils.Const;
using Shared.Infrastructure.Contexts;
using StackExchange.Redis;
using UserService.Domain.ReadModels;
using UserService.Infrastructure.Contexts;

namespace UserService.API.Extensions;

public static class DatabaseExtensions
{
    public static IServiceCollection AddDatabaseServices(this IServiceCollection services)
    {
        EnvLoader.Load();
        var connectionString = Environment.GetEnvironmentVariable(ConstEnv.UserServiceDB);
        
        services.AddSingleton<IConnectionMultiplexer>(sp =>
            ConnectionMultiplexer.Connect("localhost:6379,password=Gi@khoi221203,allowAdmin=true"));        
        services.AddScoped<IDatabase>(sp =>
            sp.GetRequiredService<IConnectionMultiplexer>().GetDatabase());
        
        // Entity Framework configuration
        services.AddDbContext<UserServiceContext>(options =>
        {
            options.UseNpgsql(connectionString);
        });
        
        services.AddScoped<AppDbContext, UserServiceContext>();
        
        // Marten document database configuration
        services.AddMarten(options =>
        {
            options.Connection(connectionString!);
            options.AutoCreateSchemaObjects = AutoCreate.All;
            options.DatabaseSchemaName = "UserServiceDB_Marten";
            
            options.Schema.For<StudentCollection>().Identity(x => x.StudentId);
            options.Schema.For<TeacherCollection>().Identity(x => x.TeacherId);
            options.Schema.For<TeacherRatingCollection>().Identity(x => x.RatingId);
        });
        
        return services;
    }
    
    public static async Task<WebApplication> EnsureDatabaseCreatedAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<UserServiceContext>();
        await db.Database.EnsureCreatedAsync();
        return app;
    }
}