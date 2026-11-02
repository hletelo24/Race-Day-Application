using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using front_end_web.Models.Auth;

namespace front_end_web.Services;

public class AuthService : IAuthService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AuthService> _logger;

    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    public AuthService(
        IHttpClientFactory httpClientFactory,
        ILogger<AuthService> logger)
    {
        _httpClient = httpClientFactory.CreateClient("RaceDayApi");
        _logger = logger;
    }

    public async Task<AuthServiceResult<ApiAuthResponse>> LoginAsync(
        LoginViewModel model,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new
            {
                model.EmailAddress,
                model.Password
            };

            using var response = await _httpClient.PostAsJsonAsync(
                "api/auth/login",
                request,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return AuthServiceResult<ApiAuthResponse>.Fail(
                    await GetErrorMessageAsync(response));
            }

            var result =
                await response.Content.ReadFromJsonAsync<ApiAuthResponse>(
                    JsonOptions,
                    cancellationToken);

            if (result?.User == null ||
                string.IsNullOrWhiteSpace(result.Token))
            {
                _logger.LogWarning(
                    "The API returned an incomplete login response.");

                return AuthServiceResult<ApiAuthResponse>.Fail(
                    "The server returned an invalid login response.");
            }

            if (result.User.UserId <= 0 ||
                (result.User.Role != "Participant" &&
                 result.User.Role != "Organiser"))
            {
                return AuthServiceResult<ApiAuthResponse>.Fail(
                    "The account details returned by the server are invalid.");
            }

            return AuthServiceResult<ApiAuthResponse>.Ok(
                result,
                result.Message ?? "Login successful.");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "RaceDay API login request failed.");

            return AuthServiceResult<ApiAuthResponse>.Fail(
                "Unable to connect to the RaceDay server. Please try again.");
        }
        catch (OperationCanceledException)
            when (!cancellationToken.IsCancellationRequested)
        {
            return AuthServiceResult<ApiAuthResponse>.Fail(
                "The server took too long to respond. Please try again.");
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Invalid JSON returned by login endpoint.");

            return AuthServiceResult<ApiAuthResponse>.Fail(
                "The server returned an unexpected response.");
        }
    }

    public async Task<AuthServiceResult<ApiAuthResponse>> RegisterAsync(
        RegisterViewModel model,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // These property names match the API's RegisterRequest.
            var request = new
            {
                model.FirstName,
                model.LastName,
                model.EmailAddress,
                model.Password,
                model.Role,
                model.Gender,
                model.Age,
                model.AccessLevel,
                ImageUrl = (string?)null
            };

            using var response = await _httpClient.PostAsJsonAsync(
                "api/auth/register",
                request,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return AuthServiceResult<ApiAuthResponse>.Fail(
                    await GetErrorMessageAsync(response));
            }

            var result =
                await response.Content.ReadFromJsonAsync<ApiAuthResponse>(
                    JsonOptions,
                    cancellationToken);

            if (result?.User == null ||
                string.IsNullOrWhiteSpace(result.Token))
            {
                _logger.LogWarning(
                    "The API returned an incomplete registration response.");

                return AuthServiceResult<ApiAuthResponse>.Fail(
                    "The server returned an invalid registration response.");
            }

            return AuthServiceResult<ApiAuthResponse>.Ok(
                result,
                result.Message ?? "Registration successful.");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "RaceDay API registration request failed.");

            return AuthServiceResult<ApiAuthResponse>.Fail(
                "Unable to connect to the RaceDay server. Please try again.");
        }
        catch (OperationCanceledException)
            when (!cancellationToken.IsCancellationRequested)
        {
            return AuthServiceResult<ApiAuthResponse>.Fail(
                "The server took too long to respond. Please try again.");
        }
        catch (JsonException ex)
        {
            _logger.LogError(
                ex,
                "Invalid JSON returned by registration endpoint.");

            return AuthServiceResult<ApiAuthResponse>.Fail(
                "The server returned an unexpected response.");
        }
    }

    private static async Task<string> GetErrorMessageAsync(
        HttpResponseMessage response)
    {
        if (response.StatusCode == HttpStatusCode.Unauthorized)
            return "Invalid email address or password.";

        if (response.StatusCode == HttpStatusCode.Conflict)
            return "An account with this email address already exists.";

        try
        {
            var body = await response.Content.ReadAsStringAsync();

            if (!string.IsNullOrWhiteSpace(body))
            {
                using var document = JsonDocument.Parse(body);
                var root = document.RootElement;

                foreach (var property in new[] { "message", "detail" })
                {
                    if (root.TryGetProperty(property, out var value) &&
                        value.ValueKind == JsonValueKind.String)
                    {
                        var message = value.GetString();

                        if (!string.IsNullOrWhiteSpace(message))
                            return message;
                    }
                }
            }
        }
        catch (JsonException)
        {
            // Do not expose an unrecognised response body to the user.
        }

        return response.StatusCode switch
        {
            HttpStatusCode.BadRequest =>
                "Please check the information you entered.",

            HttpStatusCode.TooManyRequests =>
                "Too many requests. Please wait and try again.",

            _ => "The request could not be completed. Please try again."
        };
    }
}