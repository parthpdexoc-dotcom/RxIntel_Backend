namespace Rxintel.Generic.Common.Implementations.Responses
{
    using Rxintel.Generic.Common.Exceptions;
    using Rxintel.Generic.Enums;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;

    /// <summary>
    /// Class success result for single items
    /// </summary>
    /// <typeparam name="T">Item class</typeparam>
    public class SuccessResult<T> where T : class
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SuccessResult{T}"/> class.
        /// </summary>
        public SuccessResult()
        {
            Message = string.Empty;
        }

        private SuccessResult(int status, string message, T? data = default, Exception? ex = null)
        {
            Status = status;
            Message = message;
            Data = data;
        }

        public static SuccessResult<T> Ok(T data, string message = CommonVariables.Success) =>
        new((int)HttpStatusCode.OK, message, data);

        public static SuccessResult<T> NotFound(string message = CommonVariables.NotFound) =>
            new((int)HttpStatusCode.NotFound, message);

        public static SuccessResult<T> BadRequest(string message = CommonVariables.BadRequest) =>
            new((int)HttpStatusCode.BadRequest, message);

        public static SuccessResult<T> NotUpdated(string message = CommonVariables.NotUpdated) =>
            new((int)HttpStatusCode.NotModified, message);
        /// <summary>
        /// Gets or sets the code.
        /// </summary>
        /// <value>
        /// The code.
        /// </value>
        /// <summary>
        /// Gets or sets the code.
        /// </summary>
        /// <value>
        /// The code.
        /// </value>
        public int Status { get; set; }

        /// <summary>
        /// Gets or sets the message.
        /// </summary>
        /// <value>
        /// The message.
        /// </value>
        public string Message { get; set; }

        /// <summary>
        /// Gets or sets the item.
        /// </summary>
        /// <value>
        /// The item.
        /// </value>
        public T? Data { get; set; }

        /// <summary>
        /// Validation or business errors included in API responses.
        /// </summary>
        public List<ErrorDetail>? Errors { get; set; }

        /// <summary>
        /// Gets or sets the identifier.
        /// </summary>
        /// <value>
        /// The identifier.
        /// </value>
        public int Id { get; set; }
        /// <summary>
        /// Gets a value indicating whether this instance is valid.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is valid; otherwise, <c>false</c>.
        /// </value>
        public bool IsValid
        {
            get
            {
                if (Data is IEnumerable<T> enumerable)
                {
                    return enumerable.Any();
                }

                return Data != null;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this instance is success status code.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is success status code; otherwise, <c>false</c>.
        /// </value>
        public bool IsSuccessStatusCode
        {
            get
            {
                return Status >= (int)HttpStatusCode.OK && Status < (int)HttpStatusCode.Ambiguous;
            }
        }
    }
}
