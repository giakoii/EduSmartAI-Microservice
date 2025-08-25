using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using OpenIddict.Abstractions;
using Shared.Application.Interfaces.IdentityHepers;

namespace Shared.Infrastructure.Identities;

public class IdentityService(IHttpContextAccessor httpContextAccessor) : IIdentityService
{
    /// <summary>
    /// Get identity from ClaimsPrincipal
    /// </summary>
    /// <param name="user"></param>
    /// <returns></returns>
    public IdentityEntity? GetIdentity(ClaimsPrincipal user)
    {
        var identity = user.Identity as ClaimsIdentity;
        if (identity is { IsAuthenticated: false })
            return null;
        
        // Get id
        var id = identity!.FindFirst(OpenIddictConstants.Claims.Subject)!.Value;
        
        // Get email
        var email = identity.FindFirst(OpenIddictConstants.Claims.Email)!.Value;
        
        // Get name
        var name = identity.FindFirst(OpenIddictConstants.Claims.Name)?.Value;
        
        // Get the role
        var role = identity.FindFirst(OpenIddictConstants.Claims.Role)?.Value;
        
        // Create IdentityEntity
        var identityEntity = new IdentityEntity
        {
            UserId = Guid.Parse(id),
            Email = email,
            FullName = name!,
            RoleName = role!,
        };
        return identityEntity;
    }

    /// <summary>
    /// Get current user from HttpContext
    /// </summary>
    /// <returns></returns>
    public IdentityEntity? GetCurrentUser()
    {
        var user = httpContextAccessor.HttpContext?.User;
        if (user != null) 
            return GetIdentity(user);
        return null;
    }
}