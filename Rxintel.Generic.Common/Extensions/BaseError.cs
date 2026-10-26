using System.Text.Json.Serialization;

namespace Rxintel.Generic.Common.Extensions;

public abstract class BaseError
{
    [JsonPropertyName("code")]
    public int Code { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }
}
