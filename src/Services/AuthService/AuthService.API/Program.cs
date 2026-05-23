namespace AuthService.API;

using System.Text;
using AuthService.API.Endpoints;
using AuthService.API.Extensions;
using AuthService.Application.Commands.Auth;
using AuthService.Application.Validators;
using AuthService.Infrastructure.Persistence;
using FluentValidation;
using MassTransit;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

public static class Program
{
    public static int Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        ConfigureDevDefaults(builder.Configuration);

        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                var issuer = builder.Configuration["AuthJwt:Issuer"] ?? "PhotoAppBackend.AuthService";
                var audience = builder.Configuration["AuthJwt:Audience"] ?? "PhotoAppBackend";
                var signingKey = builder.Configuration["AuthJwt:SigningKey"]
                    ?? throw new InvalidOperationException("AuthJwt:SigningKey is required.");

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = issuer,
                    ValidateAudience = true,
                    ValidAudience = audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(1)
                };
            });

        builder.Services.AddAuthorization();

        var frontendDevOrigin = builder.Configuration["Frontend:DevOrigin"] ?? "http://localhost:5173";
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("DefaultCors", policy =>
            {
                policy.WithOrigins(frontendDevOrigin)
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            });
        });

        builder.Services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor
                | ForwardedHeaders.XForwardedProto
                | ForwardedHeaders.XForwardedHost;
            options.KnownNetworks.Clear();
            options.KnownProxies.Clear();
            options.ForwardLimit = 1;
        });

        builder.Services.AddDbContext<AuthServiceDbContext>(options =>
        {
            var connectionString = builder.Configuration.GetConnectionString("AuthService")
                ?? "Host=localhost;Port=5432;Database=authservice;Username=photoapp_user;Password=photoapp_secure_password_123!";

            options.UseNpgsql(connectionString);
        });

        builder.Services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(LoginCommand).Assembly));

        builder.Services.AddValidatorsFromAssemblyContaining<LoginCommandValidator>();
        builder.Services.AddAuthApplicationServices();
        builder.Services.AddAuthInfrastructureServices(builder.Configuration);

        var app = builder.Build();

        using (var scope = app.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AuthServiceDbContext>();
            dbContext.Database.Migrate();
        }

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseForwardedHeaders();
        app.UseHttpsRedirection();
        app.UseCors("DefaultCors");
        app.UseAuthentication();
        app.UseAuthorization();

        app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "AuthService" }));
        app.MapAuthEndpoints();

        app.Run();
        return 0;
    }

    private static void ConfigureDevDefaults(IConfiguration configuration)
    {
        Environment.SetEnvironmentVariable(
            "AUTH_JWT_SIGNING_KEY",
            configuration["AuthJwt:SigningKey"] ?? "photoapp-auth-service-dev-signing-key-32-plus-chars!!");

        Environment.SetEnvironmentVariable(
            "AUTH_JWT_ISSUER",
            configuration["AuthJwt:Issuer"] ?? "PhotoAppBackend.AuthService");

        Environment.SetEnvironmentVariable(
            "AUTH_JWT_AUDIENCE",
            configuration["AuthJwt:Audience"] ?? "PhotoAppBackend");
    }
}