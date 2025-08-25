using ReverseProxy.Configurations;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddReverseProxy()
    .LoadFromMemory(RouteConfiguration.GetRoutes(), ClusterConfiguration.GetClusters());

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseRouting();
app.UseHttpsRedirection();

// Map reverse proxy first
app.MapReverseProxy();

// Then configure Swagger UI
app.UseSwaggerUi(settings =>
{
    settings.SwaggerRoutes.Add(new NSwag.AspNetCore.SwaggerUiRoute("Auth Service Swagger", "/auth/swagger/v1/swagger.json"));
    settings.SwaggerRoutes.Add(new NSwag.AspNetCore.SwaggerUiRoute("User Service Swagger", "/user/swagger/v1/swagger.json"));
    settings.Path = "/swagger";
});

app.Run();
