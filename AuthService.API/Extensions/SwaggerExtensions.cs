using System.Net;
using NSwag;
using NSwag.Generation.Processors.Security;

namespace AuthService.API.Extensions;

public static class SwaggerExtensions
{
    public static IServiceCollection AddSwaggerServices(this IServiceCollection services)
    {
        services.AddOpenApi();
        
        services.AddOpenApiDocument(config =>
        {
            config.OperationProcessors.Add(new OperationSecurityScopeProcessor("JWT_Token"));
            config.AddSecurity("JWT_Token", [],
                new OpenApiSecurityScheme()
                {
                    Type = OpenApiSecuritySchemeType.ApiKey,
                    Name = nameof(Authorization),
                    In = OpenApiSecurityApiKeyLocation.Header,
                    Description = "Copy this into the value field: Bearer {token}",
                }
            );
            config.Title = "Auth Service";
            config.Version = "v1";
        });
        
        return services;
    }
}