using BuildingBlocks.Messaging.Events.UserLoginEvents;
using MassTransit;
using MediatR;
using StudentService.Application.Users.Queries.Logins;

namespace StudentService.Application.Users.Consumers;

public class UserLoginEventConsumer(IMediator mediator) : IConsumer<UserLoginEvent>
{
    public async Task Consume(ConsumeContext<UserLoginEvent> context)
    {
        var evt = context.Message;
        
        var command = new UserLoginQuery
        {
            UserId = evt.UserId
        };
            
        var response = await mediator.Send(command);
        
        await context.RespondAsync(response);
    }
}