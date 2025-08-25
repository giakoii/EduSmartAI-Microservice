using Shared.Common.ApiEntities;

namespace BuildingBlocks.Messaging.Events.InsertUserEvents;

public record UserInsertEventResponse : AbstractApiResponse<string>
{
    public override string Response { get; set; }
}