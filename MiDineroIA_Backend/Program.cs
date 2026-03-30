using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MiDineroIA_Backend.Application.Interfaces;
using MiDineroIA_Backend.Application.Services;
using MiDineroIA_Backend.CrossCutting.Auth;
using MiDineroIA_Backend.Domain.Interfaces;
using MiDineroIA_Backend.Infrastructure.Database;
using MiDineroIA_Backend.Infrastructure.Repositories;
using MiDineroIA_Backend.Infrastructure.Security;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

// Database
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        Environment.GetEnvironmentVariable("SQL_CONNECTION_STRING")));

// Security
builder.Services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();
builder.Services.AddSingleton(JwtSettings.FromEnvironment());
builder.Services.AddSingleton<JwtTokenGenerator>();
builder.Services.AddSingleton<ITokenGenerator>(sp => sp.GetRequiredService<JwtTokenGenerator>());
builder.Services.AddSingleton<ITokenValidator>(sp => sp.GetRequiredService<JwtTokenGenerator>());

// Auth
builder.Services.AddSingleton<JwtHelper>();

// Repositories
builder.Services.AddScoped<IUserRepository, UserRepository>();

// Services
builder.Services.AddScoped<IAuthService, AuthService>();

builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

builder.Build().Run();
