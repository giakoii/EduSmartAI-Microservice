using Shared.Common.ApiEntities;

namespace Shared.Application.Interfaces.Commons;

public record DecryptTextResponse : AbstractApiResponse<DecryptTextResponseEntity>
{
    public override DecryptTextResponseEntity Response { get; set; }
}

public class DecryptTextResponseEntity
{
    public Guid Id { get; set; }
    public string Email { get; set; }
}