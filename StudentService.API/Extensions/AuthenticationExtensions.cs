using BuildingBlocks.Messaging.Settings;
using OpenIddict.Validation.AspNetCore;
using Shared.Common.Utils.Const;

namespace StudentService.API.Extensions;

public static class AuthenticationExtensions
{
    public static IServiceCollection AddAuthenticationServices(this IServiceCollection services)
    {
        EnvLoader.Load();
        services.AddAuthentication(options =>
       {
           options.DefaultScheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
       });

       // Configure token validation
       services.AddOpenIddict()
           .AddValidation(options =>
           {
               options.SetIssuer("http://localhost:5050");
               options.AddAudiences("service_client");

               options.UseIntrospection()
                   .AddAudiences("service_client")
                   .SetClientId("service_client")
                   .SetClientSecret(Environment.GetEnvironmentVariable(ConstEnv.ClientSecret)!);

               options.UseSystemNetHttp();
               options.UseAspNetCore();
           });
        return services;
    }
}