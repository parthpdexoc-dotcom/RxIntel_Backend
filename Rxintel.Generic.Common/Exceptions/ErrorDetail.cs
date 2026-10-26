using Rxintel.Generic.Enums;

namespace Rxintel.Generic.Common.Exceptions;

public class ErrorDetail
{
    public ErrorType ErrorType { get; set; }
    public string ErrorCode { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    public string FieldName { get; set; } = string.Empty;

    public ErrorDetail()
    {
    }

    public ErrorDetail(string errorMessage, ErrorType errorType, string errorCode, string fieldName)
    {
        ErrorType = errorType;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
        FieldName = fieldName;
    }
}
