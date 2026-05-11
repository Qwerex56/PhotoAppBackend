namespace AuthService.API.Extensions;

using AuthService.Application.Services;
using AuthService.Domain.Repositories;
using AuthService.Infrastructure.Repositories;
using MassTransit;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAuthApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IPasswordHasher, Argon2PasswordHasher>();
        services.AddScoped<ITokenService, JwtTokenService>();
        services.AddScoped<IEmailTokenService, EmailTokenService>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<IPasswordValidator, PasswordValidator>();

        return services;
    }

    public static IServiceCollection AddAuthInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IAuthUnitOfWork, AuthUnitOfWork>();
        services.AddScoped<IUserRepository>(sp => sp.GetRequiredService<IAuthUnitOfWork>().Users);
        services.AddScoped<IRefreshTokenRepository>(sp => sp.GetRequiredService<IAuthUnitOfWork>().RefreshTokens);
        services.AddScoped<IEmailVerificationTokenRepository>(sp => sp.GetRequiredService<IAuthUnitOfWork>().EmailVerificationTokens);
        services.AddScoped<IPasswordResetTokenRepository>(sp => sp.GetRequiredService<IAuthUnitOfWork>().PasswordResetTokens);

        services.AddMassTransit(x =>
        {
            x.UsingRabbitMq((context, cfg) =>
            {
                var host = configuration["RabbitMQ:Host"] ?? "localhost";
                var virtualHost = configuration["RabbitMQ:VirtualHost"] ?? "/photoapp";
                var username = configuration["RabbitMQ:Username"] ?? "photoapp_user";
                var password = configuration["RabbitMQ:Password"] ?? "photoapp_secure_password_123!";

                cfg.Host(host, virtualHost, hostConfigurator =>
                {
                    hostConfigurator.Username(username);
                    hostConfigurator.Password(password);
                });
            });
        });

        return services;
    }
}