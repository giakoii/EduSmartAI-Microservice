using MediatR;
using Microsoft.AspNetCore.Mvc;
using NLog;
using Shared.Application.Utils.Const;
using Shared.Common.ApiEntities;
using Shared.Common.Utils;

namespace Shared.API.AbstractControllers;

public abstract class AbstractApiAsyncControllerNotToken
    <TRequest, TResponse, TEntityResponse>: ControllerBase
    where TRequest : class
    where TResponse : AbstractApiResponse<TEntityResponse>
{
    protected IMediator _mediator;
    
    /// <summary>
    /// API entry point
    /// </summary>
    public abstract Task<TResponse> ProcessRequest(TRequest request);

    /// <summary>
    /// TemplateMethod
    /// </summary>
    /// <param name="request"></param>
    /// <param name="logger"></param>
    /// <param name="returnValue"></param>
    /// <returns></returns>
    protected async Task<TResponse> ProcessRequest(TRequest request, Logger logger, TResponse returnValue)
    {
        var loggingUtil = new LoggingUtil(logger, "System");
        loggingUtil.StartLog(request);
        try
        {
            // Error check
            var detailErrorList = AbstractFunction<TResponse, TEntityResponse>.ErrorCheck(this.ModelState);
            returnValue = ErrorCheck(detailErrorList, returnValue);

            // If there is no error, execute the main process
            if (returnValue.Success)
                returnValue = await Exec(request);
        }
        catch (Exception e)
        {
            loggingUtil.ErrorLog(e.Message);
            return AbstractFunction<TResponse, TEntityResponse>.GetReturnValue(returnValue, loggingUtil, e);
        }

        // Processing end log
        loggingUtil.EndLog(returnValue);
        return returnValue;
    }
    
    /// <summary>
    /// Error check
    /// </summary>
    private TResponse ErrorCheck(List<DetailError> detailErrors, TResponse returnValue)
    {
        detailErrors = AbstractFunction<TResponse, TEntityResponse>.ErrorCheck(this.ModelState);
        if (detailErrors.Count > 0)
        {
            returnValue.Success = false;
            returnValue.SetMessage(MessageId.E10000);
            returnValue.DetailErrors = detailErrors;
        }
        else
        {
            returnValue.Success = true;
        }

        return returnValue;
    }

    /// <summary>
    /// Main processing (to be implemented in derived classes)
    /// </summary>
    private async Task<TResponse> Exec(TRequest request)
    {
        var result = await _mediator.Send(request);
        return (TResponse)result!;
    }
}