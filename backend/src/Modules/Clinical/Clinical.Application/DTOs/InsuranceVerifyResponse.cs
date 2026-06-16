using System.Text.Json;
using System.Text.Json.Serialization;

namespace Clinical.Application.DTOs;

public sealed record InsuranceVerifyResponse(
    [property: JsonConverter(typeof(LowercaseEnumConverter<InsuranceVerifyStatus>))]
    InsuranceVerifyStatus Status,
    string InsuranceName,
    string Message,
    string? Details = null);

[JsonConverter(typeof(LowercaseEnumConverter<InsuranceVerifyStatus>))]
public enum InsuranceVerifyStatus
{
    Verified,
    Partial,
    Unrecognized,
    Unavailable
}

public sealed class LowercaseEnumConverter<T> : JsonStringEnumConverter<T> where T : struct, Enum
{
    public LowercaseEnumConverter() : base(JsonNamingPolicy.CamelCase) { }
}
