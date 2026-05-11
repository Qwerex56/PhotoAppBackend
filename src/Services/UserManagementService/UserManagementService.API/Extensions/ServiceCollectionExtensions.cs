namespace UserManagementService.API.Extensions;

using FluentValidation;
using MassTransit;
using MediatR;
using Microsoft.EntityFrameworkCore;
using UserManagementService.Application;
using UserManagementService.Application.Validators;
using UserManagementService.Domain.Repositories;
using UserManagementService.Infrastructure.Persistence;
using UserManagementService.Infrastructure.Repositories;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddUserManagementApplicationServices(this IServiceCollection services)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(AssemblyReference).Assembly));
        services.AddValidatorsFromAssemblyContaining<UpdateProfileCommandValidator>();

        return services;
    }

    public static IServiceCollection AddUserManagementInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<UserManagementDbContext>(options =>
        {
            var connectionString = configuration.GetConnectionString("UserManagementService")
                ?? "Host=localhost;Port=5432;Database=usermanagementservice;Username=photoapp_user;Password=photoapp_secure_password_123!";

            options.UseNpgsql(connectionString);
        });

        services.AddScoped<IUserManagementUnitOfWork, UserManagementUnitOfWork>();

        services.AddMassTransit(x =>
        {
            x.AddConsumers(typeof(AssemblyReference).Assembly);

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

                cfg.ConfigureEndpoints(context);
            });
        });

        return services;
    }
}