using BuildingBlocks.Messaging.Events.InsertUserEvents;
using MassTransit;
using UtilityService.Application.Interfaces;

namespace UtilityService.Application.Consumers;

public class SendKeyEventConsumer : IConsumer<SendKeyEvent>
{
    private readonly ISendmailService _sendmailService;

    public SendKeyEventConsumer(ISendmailService sendmailService)
    {
        _sendmailService = sendmailService;
    }

    public Task Consume(ConsumeContext<SendKeyEvent> context)
    {
        var key = context.Message.Key;
        
        //
        return null;
    }
}