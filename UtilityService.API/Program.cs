using Microsoft.AspNetCore.Mvc;
using Shared.Common.Settings;
using UtilityService.API.Extensions;

EnvLoader.Load();
var builder = WebApplication.CreateBuilder(args);

// Configure core services
builder.Services.AddOpenApi();
builder.Services.AddControllers();

// Configure services using extension methods
builder.Services.AddDatabaseServices();
builder.Services.AddRepositoryServices();
builder.Services.AddSwaggerServices();
builder.Services.AddCorsServices();

builder.Services.AddDataProtection();
builder.Services.AddHttpContextAccessor();

#region MVC and API behavior configuration
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.SuppressModelStateInvalidFilter = true;
});
#endregion

#region Application build and middleware pipeline
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

await app.EnsureDatabaseCreatedAsync();

app.UseCors();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseHttpsRedirection();
app.MapControllers();
app.UseSwagger();
app.UseSwaggerUI(settings =>
{
    settings.RoutePrefix = "swagger";
});
app.Run();
#endregion