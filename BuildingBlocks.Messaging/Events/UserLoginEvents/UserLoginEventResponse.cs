using Shared.Common.ApiEntities;

namespace BuildingBlocks.Messaging.Events.UserLoginEvents;

public record UserLoginEventResponse : AbstractApiResponse<UserLoginEntity>
{
    public override UserLoginEntity Response { get; set; }
}

public record UserLoginEntity(
    string FirstName,
    string LastName
);