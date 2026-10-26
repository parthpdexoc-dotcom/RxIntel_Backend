using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Rxintel.Generic.Common.Exceptions;
using Rxintel.Generic.Common.Extensions;
using Rxintel.Generic.Common.Implementations.Responses;
using System.Net;

namespace Rxintel.Generic.Models.Filter;

public class CustomExceptionFilter : Filter, IAsyncExceptionFilter
{
    public Task OnExceptionAsync(ExceptionContext context)
    {
        var (httpCode, statusCode, errorMessage, shouldTrack) = context.Exception switch
        {
            WebApiException e => (e.HttpStatusCode, (int)e.HttpStatusCode, e.ErrorMessage,
                e.HttpStatusCode == HttpStatusCode.InternalServerError),
            _ => (HttpStatusCode.InternalServerError, (int)HttpStatusCode.InternalServerError,
                context.Exception.Message, true)
        };

        context.Result = new ObjectResult(new SuccessResult<WebApiError>
        {
            Data = default!,
            Status = statusCode,
            Message = errorMessage
        })
        {
            StatusCode = (int)httpCode,
            DeclaredType = typeof(WebApiError)
        };

        if (shouldTrack)
            TrackException(context.Exception);

        return Task.CompletedTask;
    }
}
