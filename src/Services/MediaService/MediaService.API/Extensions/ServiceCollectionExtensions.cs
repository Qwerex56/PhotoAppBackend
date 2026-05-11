namespace MediaService.API.Extensions;

using FluentValidation;
using MassTransit;
using MediatR;
using Microsoft.EntityFrameworkCore;
using MediaService.Application;
using MediaService.Application.Consumers.Users;
using MediaService.Application.Services;
using MediaService.Application.Validators;
using MediaService.Domain.Repositories;
using MediaService.Infrastructure.Persistence;
using MediaService.Infrastructure.Repositories;
using Shared.Constants;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMediaApplicationServices(this IServiceCollection services)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(AssemblyReference).Assembly));
        services.AddValidatorsFromAssemblyContaining<CreateAlbumCommandValidator>();
        services.AddScoped<IMediaSecurityService, MediaSecurityService>();
        services.AddScoped<IMediaAccessService, MediaAccessService>();

        return services;
    }

    public static IServiceCollection AddMediaInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<MediaServiceDbContext>(options =>
        {
            var connectionString = configuration.GetConnectionString("MediaService")
                ?? "Host=localhost;Port=5432;Database=mediaservice;Username=photoapp_user;Password=photoapp_secure_password_123!";

            options.UseNpgsql(connectionString);
        });

        services.AddScoped<IMediaUnitOfWork, MediaUnitOfWork>();
        services.AddScoped<IAlbumRepository>(sp => sp.GetRequiredService<IMediaUnitOfWork>().Albums);
        services.AddScoped<IMediaAssetRepository>(sp => sp.GetRequiredService<IMediaUnitOfWork>().MediaAssets);
        services.AddScoped<IMediaTagRepository>(sp => sp.GetRequiredService<IMediaUnitOfWork>().MediaTags);
        services.AddScoped<IAlbumShareRepository>(sp => sp.GetRequiredService<IMediaUnitOfWork>().AlbumShares);
        services.AddScoped<IMediaShareRepository>(sp => sp.GetRequiredService<IMediaUnitOfWork>().MediaShares);

        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = configuration["Redis:ConnectionString"]
                ?? "localhost:6379,password=photoapp_secure_password_123!,abortConnect=false";
            options.InstanceName = "photoapp:media:";
        });

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

                cfg.ReceiveEndpoint(MessageQueues.MediaServiceQueue, endpoint =>
                {
                    endpoint.ConfigureConsumer<UserDeletedEventConsumer>(context);
                });
            });
        });

        return services;
    }
}