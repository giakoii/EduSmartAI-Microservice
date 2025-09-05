using Microsoft.AspNetCore.Mvc;
using Shared.Common.Settings;
using StudentService.API.Extensions;
using StudentService.Infrastructure.Contexts;

EnvLoader.Load();
var builder = WebApplication.CreateBuilder(args);

// Configure core services
builder.Services.AddOpenApi();
builder.Services.AddControllers();

// Configure services using extension methods
builder.Services.AddDatabaseServices();
builder.Services.AddAuthenticationServices();
builder.Services.AddRepositoryServices();
builder.Services.AddMessagingServices();
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
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<UserServiceContext>();
    await dbContext.Database.EnsureCreatedAsync();
}
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseDeveloperExceptionPage();
}

app.UseCors();
app.UseRouting();
app.UsePathBase("/user");
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