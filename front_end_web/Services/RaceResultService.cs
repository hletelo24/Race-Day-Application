using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using front_end_web.Models.Results;

namespace front_end_web.Services;

public class RaceResultService : IRaceResultService
{
    private readonly IHttpClientFactory _factory;
    private readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);
    public RaceResultService(IHttpClientFactory factory) => _factory = factory;

    private HttpClient Client(string token)
    {
        var client = _factory.CreateClient("RaceDayApi");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    public async Task<List<RaceResultViewModel>> GetEventResultsAsync(int eventId, string token, CancellationToken ct = default)
    {
        using var client = Client(token);
        using var response = await client.GetAsync($"api/results/event/{eventId}", ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<RaceResultViewModel>>(_json, ct) ?? new();
    }

    public async Task<(bool Success, string Message)> SaveResultAsync(int enrolmentId, string timeText, string token, CancellationToken ct = default)
    {
        if (!TimeSpan.TryParseExact(timeText, new[] { @"h\:mm\:ss", @"hh\:mm\:ss", @"hhh\:mm\:ss" },
            CultureInfo.InvariantCulture, out var time) || time <= TimeSpan.Zero)
            return (false, "Enter a valid finish time in HH:MM:SS format.");

        using var client = Client(token);
        using var response = await client.PostAsJsonAsync("api/results",
            new { EnrolmentId = enrolmentId, Time = time.ToString(@"hh\:mm\:ss", CultureInfo.InvariantCulture) }, ct);
        if (response.IsSuccessStatusCode) return (true, "Race result saved successfully.");
        var body = await response.Content.ReadAsStringAsync(ct);
        try
        {
            using var json = JsonDocument.Parse(body);
            if (json.RootElement.TryGetProperty("message", out var msg))
                return (false, msg.GetString() ?? $"Request failed ({(int)response.StatusCode}).");
        }
        catch (JsonException) { }
        return (false, $"Request failed ({(int)response.StatusCode}).");
    }

    public async Task<List<ParticipantRaceResultViewModel>> GetMyResultsAsync(string token, CancellationToken ct = default)
    {
        using var client = Client(token);
        using var response = await client.GetAsync("api/results/my-results", ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<ParticipantRaceResultViewModel>>(_json, ct) ?? new();
    }
}
