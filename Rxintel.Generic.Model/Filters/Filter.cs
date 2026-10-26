using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Rxintel.Generic.Common.Exceptions;
using Rxintel.Generic.Common.Extensions;
using Rxintel.Generic.Common.Implementations.Responses;
using Rxintel.Generic.Enums;
using Rxintel.Generic.Models.Constants;
using Rxintel.Logger.Logger;
using System.Net;
using System.Reflection;

namespace Rxintel.Generic.Models.Filter;

public class Filter : ResultFilterAttribute
{
    private readonly IRxintelLogger _logger =
        RxintelLogManager.GetLogger(MethodBase.GetCurrentMethod()?.DeclaringType!);

    protected void TrackException(Exception ex)
    {
        _logger.Error("Unhandled exception. trace_id {TraceIdentifier}", ex);
    }

    public override void OnResultExecuting(ResultExecutingContext context)
    {
        base.OnResultExecuting(context);

        if (context.ModelState.IsValid)
            return;

        var allErrors = context.ModelState
            .Where(ms => ms.Value?.ValidationState == ModelValidationState.Invalid)
            .SelectMany(ms => ms.Value!.Errors.Select(err =>
            {
                var resolved = ModelValidationErrorCodes.GetErrorMessage(err.ErrorMessage);
                var errorMessage = string.IsNullOrWhiteSpace(resolved)
                    ? err.ErrorMessage
                    : resolved;
                return new ErrorDetail(
                    errorMessage: errorMessage,
                    errorType: ErrorType.Error,
                    errorCode: err.ErrorMessage,
                    fieldName: ms.Key);
            }))
            .ToList();

        var errorMessage = allErrors
            .Select(e => e.ErrorMessage)
            .FirstOrDefault(msg => !string.IsNullOrWhiteSpace(msg))
            ?? ModelValidationErrorCodes.GetErrorMessage(ModelValidationErrorCodes.INVALID_REQUEST);
        var webApiError = new SuccessResult<WebApiError>
        {
            Data = null!,
            Status = (int)HttpStatusCode.BadRequest,
            Message = errorMessage,
            Errors = allErrors
        };

        context.Result = new ObjectResult(webApiError)
        {
            StatusCode = (int)HttpStatusCode.BadRequest,
            DeclaredType = typeof(WebApiError)
        };
    }
}
