using NSwag.AspNetCore;

namespace ReverseProxy.Extensions;

public static class SwaggerUiExtension
{
    /// <summary>
    /// Configure Swagger UI for Reverse Proxy
    /// </summary>
    public static void ConfigureSwaggerUi(this IApplicationBuilder app)
    {
        app.UseSwaggerUi(settings =>
        {
            settings.Path = "/swagger";
            
            // Add Swagger routes for AuthService
            settings.SwaggerRoutes.Add(new SwaggerUiRoute(
                "Auth Service Swagger", 
                "/auth/swagger/v1/swagger.json"
            ));
            
            // Add Swagger routes for UserService
            settings.SwaggerRoutes.Add(new SwaggerUiRoute(
                "User Service Swagger", 
                "/user/swagger/v1/swagger.json"
            ));
        });
    }
}