using BuildingBlocks.CQRS;
using BuildingBlocks.Messaging.Events.UserLoginEvents;

namespace UserService.Application.Users.Queries.Logins;

public record UserLoginQuery : IQuery<UserLoginEventResponse>
{
    public Guid UserId { get; set; }
}