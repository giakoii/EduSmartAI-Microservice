using Microsoft.AspNetCore.Mvc;
using Shared.Application.Interfaces;
using Shared.Application.Interfaces.Commons;
using Shared.Application.Interfaces.IdentityHepers;
using Shared.Application.Interfaces.Repositories;
using Shared.Common.Settings;
using Shared.Infrastructure.Identities;
using Shared.Infrastructure.Logics;
using Shared.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddHttpContextAccessor();

builder.Services.AddScoped<IIdentityService, IdentityService>();
builder.Services.AddScoped(typeof(ICommandRepository<>), typeof(CommandRepository<>));
builder.Services.AddScoped(typeof(IQueryRepository<>), typeof(QueryRepository<>));
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

builder.Services.AddScoped<ICommonLogic, CommonLogic>();

builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.SuppressModelStateInvalidFilter = true;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.Run();