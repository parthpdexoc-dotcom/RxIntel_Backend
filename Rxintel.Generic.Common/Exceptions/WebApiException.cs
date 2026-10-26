using System.Net;

namespace Rxintel.Generic.Common.Exceptions
{
    public class WebApiException : Exception
    {
        public HttpStatusCode HttpStatusCode { get; }
        public string ErrorMessage { get; }

        public WebApiException(HttpStatusCode httpStatusCode, string errorMessage)
            : base(errorMessage)
        {
            HttpStatusCode = httpStatusCode;
            ErrorMessage = errorMessage;
        }
    }
}
