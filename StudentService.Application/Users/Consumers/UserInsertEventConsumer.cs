using BuildingBlocks.Messaging.Events.InsertUserEvents;
using MassTransit;
using MediatR;
using StudentService.Application.Users.Commands.Inserts;

namespace StudentService.Application.Users.Consumers;

public class UserInsertEventConsumer(IMediator mediator) : IConsumer<UserInsertEvent>
{
    public async Task Consume(ConsumeContext<UserInsertEvent> context)
    {
        var evt = context.Message;
        
        var command = new UserInsertCommand
        {
            UserId = evt.UserId,
            OldUserId = evt.OldUserId,
            Enail = evt.Email,
            FirstName = evt.FirstName,
            LastName = evt.LastName,
            UserRole = evt.UserRole
        };
            
        var response = await mediator.Send(command);
        
        await context.RespondAsync(response);
    }
}