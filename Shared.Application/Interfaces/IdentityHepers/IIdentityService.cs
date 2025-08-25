using System.Security.Claims;

namespace Shared.Application.Interfaces.IdentityHepers;

public interface IIdentityService
{
    IdentityEntity? GetIdentity(ClaimsPrincipal user);
    
    IdentityEntity? GetCurrentUser();
}

