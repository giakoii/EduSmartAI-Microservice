namespace UtilityService.Application.Interfaces;

public interface ISendmailService
{
    void VerifyEmail(string key);
}