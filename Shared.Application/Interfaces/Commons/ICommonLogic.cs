using Shared.Common.ApiEntities;

namespace Shared.Application.Interfaces.Commons;

public interface ICommonLogic
{
    EncryptTextResponse EncryptText(string beforeEncrypt);
    DecryptTextResponse DecryptText(string beforeDecrypt);
    string GenerateRandomPassword(int length = 12);
    string GenerateOtp();
}

