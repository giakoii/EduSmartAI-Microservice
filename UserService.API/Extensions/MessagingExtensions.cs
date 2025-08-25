using BuildingBlocks.Messaging.Events.InsertUserEvents;
using BuildingBlocks.Messaging.Events.UserLoginEvents;
using MassTransit;
using UserService.Application.Users.Consumers;

namespace UserService.API.Extensions;

public static class MessagingExtensions
{
    public static IServiceCollection AddMessagingServices(this IServiceCollection services)
    {
        services.AddMassTransit(x =>
        {
            x.AddConsumer<UserInsertEventConsumer>();
            x.AddConsumer<UserLoginEventConsumer>();

            x.UsingRabbitMq((context, cfg) =>
            {
                cfg.ConfigureEndpoints(context);
            });

            x.AddRequestClient<UserInsertEvent>();
            x.AddRequestClient<UserLoginEvent>();
        });
        
        return services;
    }
}