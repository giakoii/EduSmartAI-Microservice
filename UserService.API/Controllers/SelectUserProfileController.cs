using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NLog;
using OpenIddict.Validation.AspNetCore;
using Shared.API.AbstractControllers;
using Shared.Application.Interfaces.IdentityHepers;
using UserService.Application.Users.Queries.SelectUserProfile;

namespace UserService.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class SelectUserProfileController : AbstractApiAsyncController<UserProfileSelectQuery, UserProfileSelectResponse, UserProfileSelectEntity>
{
    private readonly Logger _logger = LogManager.GetCurrentClassLogger();

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="mediator"></param>
    /// <param name="identityService"></param>
    public SelectUserProfileController(IMediator mediator, IIdentityService identityService)
    {
        _mediator = mediator;
        _identityService = identityService;
    }

    [HttpGet]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    public override async Task<UserProfileSelectResponse> ProcessRequest([FromQuery]UserProfileSelectQuery request)
    {
        return await ProcessRequest(request, _logger, new UserProfileSelectResponse());
    }
}