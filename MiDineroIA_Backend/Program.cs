using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MiDineroIA_Backend.Application.Mapping;
using MiDineroIA_Backend.Application.Services;
using MiDineroIA_Backend.CrossCutting.Auth;
using MiDineroIA_Backend.Domain.Interfaces;
using MiDineroIA_Backend.Infrastructure.Database;
using MiDineroIA_Backend.Infrastructure.Repositories;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

// Database
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        Environment.GetEnvironmentVariable("SQL_CONNECTION_STRING")));

// Auth
builder.Services.AddSingleton<JwtHelper>();

// Mappers
builder.Services.AddSingleton<UserMapper>();

// Repositories
builder.Services.AddScoped<IUserRepository, UserRepository>();

// Services
builder.Services.AddScoped<AuthService>();

builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

builder.Build().Run();
