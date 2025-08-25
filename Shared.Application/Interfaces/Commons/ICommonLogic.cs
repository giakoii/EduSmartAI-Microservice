namespace Shared.Application.Interfaces.Commons;

public interface ICommonLogic
{
    string EncryptText(string beforeEncrypt);
    string DecryptText(string beforeDecrypt);
    string GenerateRandomPassword(int length = 12);
    string GenerateOtp();
}