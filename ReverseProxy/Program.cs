using ReverseProxy.Authorizations;
using ReverseProxy.Configurations;
using ReverseProxy.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add Controllers
builder.Services.AddControllers();

// Add Authentication with OpenIdConnect/JWT
builder.Services.AddReverseProxyAuthentication(builder.Configuration);

// Add YARP Reverse Proxy (routes & clusters)
builder.Services.AddReverseProxy()
    .LoadFromMemory(RouteConfiguration.GetRoutes(), ClusterConfiguration.GetClusters());

// Add Role Authorization service
builder.Services.AddSingleton<IRoleAuthorizationService, RoleAuthorizationService>();

var app = builder.Build();

app.UseRouting();
app.Use(async (context, next) =>
{
    context.Request.EnableBuffering();
    await next();
});

app.UseAuthentication();
app.UseAuthorization();
app.UseHttpsRedirection();
app.UseMiddleware<RoleAuthorizationMiddleware>();

// Enable middleware to serve generated Swagger as a JSON endpoint and the Swagger UI
if (app.Environment.IsDevelopment())
{
    app.UseOpenApi();
    app.ConfigureSwaggerUi();
}

app.MapControllers();
app.MapReverseProxy();
app.Run();