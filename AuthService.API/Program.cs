using AuthService.API;
using AuthService.API.Extensions;
using AuthService.API.Helpers;
using Microsoft.AspNetCore.Mvc;
using Shared.Common.Settings;

// Load environment variables
EnvLoader.Load();

var builder = WebApplication.CreateBuilder(args);

// Core services
builder.Services.AddControllers();
builder.Services.AddHostedService<Worker>();
builder.Services.AddHostedService<RoleSeederHostedService>();

// Configure services using extension methods
builder.Services.AddDatabaseServices();
builder.Services.AddRepositoryServices();
builder.Services.AddMessagingServices();
builder.Services.AddSwaggerServices();
builder.Services.AddCorsServices();
builder.Services.AddAuthenticationServices();
builder.Services.AddHttpContextAccessor();

builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.SuppressModelStateInvalidFilter = true;
});

var app = builder.Build();

// Ensure the database is created
await app.EnsureDatabaseCreatedAsync();

// Configure middleware pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseDeveloperExceptionPage();
}

app.UseCors();
app.UsePathBase("/auth");
app.UseRouting();
app.UseAuthentication();
app.UseStatusCodePages();
app.UseAuthorization();
app.UseHttpsRedirection();
app.MapControllers();
app.UseOpenApi();
app.UseSwaggerUi(settings =>
    {
        settings.Path = "/swagger";
    });
app.Run();
