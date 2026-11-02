using System.Text.Json.Serialization;

namespace front_end_web.Models.Auth;

public class ApiAuthResponse
{
    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("token")]
    public string? Token { get; set; }

    [JsonPropertyName("user")]
    public ApiUser? User { get; set; }
}