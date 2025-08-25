using BuildingBlocks.Messaging.Settings;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;

namespace BuildingBlocks.Messaging.Extensions;

public static class MassTransitConfiguration
{
    public static IServiceCollection AddMassTransitWithRabbitMQ(this IServiceCollection services, params Type[] consumerTypes)
    {
        EnvLoader.Load();
        
        // Load RabbitMQ settings from environment variables
        var rabbitMqHost = Environment.GetEnvironmentVariable("RABBITMQ_HOST");
        var rabbitMqUsername = Environment.GetEnvironmentVariable("RABBITMQ_USERNAME");
        var rabbitMqPassword = Environment.GetEnvironmentVariable("RABBITMQ_PASSWORD");
        
        services.AddMassTransit(x =>
        {
            foreach (var consumer in consumerTypes)
            {
                x.AddConsumer(consumer);
            }

            x.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host(rabbitMqHost, "/", h =>
                {
                    h.Username(rabbitMqUsername!);
                    h.Password(rabbitMqPassword!);
                });

                foreach (var consumer in consumerTypes)
                {
                    var messageType = consumer.GetInterfaces()
                        .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IConsumer<>))
                        ?.GetGenericArguments()[0];

                    if (messageType != null)
                    {
                        cfg.ReceiveEndpoint(messageType.Name + "-queue", e =>
                        {
                            e.ConfigureConsumer(context, consumer);
                        });
                    }
                }
            });
        });

        return services;
    }}