using BuildingBlocks.CQRS;

namespace StudentService.Application.Users.Queries.SelectUserProfile;

public record UserProfileSelectQuery() : IQuery<UserProfileSelectResponse>;
