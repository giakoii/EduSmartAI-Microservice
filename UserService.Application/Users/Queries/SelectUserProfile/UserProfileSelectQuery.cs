using BuildingBlocks.CQRS;

namespace UserService.Application.Users.Queries.SelectUserProfile;

public record UserProfileSelectQuery() : IQuery<UserProfileSelectResponse>;
