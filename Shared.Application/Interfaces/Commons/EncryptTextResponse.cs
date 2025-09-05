using Shared.Common.ApiEntities;

namespace Shared.Application.Interfaces.Commons;

public record EncryptTextResponse : AbstractApiResponse<EncryptTextResponseEntity>
{
    public override EncryptTextResponseEntity Response { get; set; }
}

public class EncryptTextResponseEntity
{
    public string EncryptedKey { get; set; }
}