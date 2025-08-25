using AuthService.Application.Accounts.Commands.Inserts;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using NLog;
using Shared.API.AbstractControllers;

namespace AuthService.API.Controllers;

/// <summary>
/// InsertStudentController - Insert student account
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
public class InsertStudentController : AbstractApiAsyncControllerNotToken<StudentInsertCommand, StudentInsertResponse, string>
{
    private readonly Logger _logger = LogManager.GetCurrentClassLogger();

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="mediator"></param>
    public InsertStudentController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Insert student account
    /// Incoming Post
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpPost]
    public override async Task<StudentInsertResponse> ProcessRequest(StudentInsertCommand request)
    {
        return await ProcessRequest(request, _logger, new StudentInsertResponse());
    }
}