using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using front_end_web.Models.Organiser;

namespace front_end_web.Services;

public class OrganiserService : IOrganiserService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<OrganiserService> _logger;

    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    public OrganiserService(
        IHttpClientFactory httpClientFactory,
        ILogger<OrganiserService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public Task<List<OrganiserEventViewModel>> GetEventsAsync(
        string token,
        CancellationToken cancellationToken = default)
    {
        return GetListAsync<OrganiserEventViewModel>(
            "api/events", token, cancellationToken);
    }

    public Task<List<OrganiserEnrolmentViewModel>> GetEnrolmentsAsync(
        string token,
        CancellationToken cancellationToken = default)
    {
        return GetListAsync<OrganiserEnrolmentViewModel>(
            "api/enrolments", token, cancellationToken);
    }

    private async Task<List<T>> GetListAsync<T>(
        string endpoint,
        string token,
        CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient("RaceDayApi");

        using var request = new HttpRequestMessage(
            HttpMethod.Get, endpoint);

        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        try
        {
            using var response = await client.SendAsync(
                request, cancellationToken);

            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<List<T>>(
                JsonOptions, cancellationToken) ?? new List<T>();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(
                ex, "Failed to retrieve data from {Endpoint}.", endpoint);

            throw;
        }
        catch (JsonException ex)
        {
            _logger.LogError(
                ex, "Invalid JSON received from {Endpoint}.", endpoint);

            throw;
        }
    }
}