using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using front_end_web.Models.Organiser;

namespace front_end_web.Services;

public class CategoryService : ICategoryService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<CategoryService> _logger;

    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    public CategoryService(
        IHttpClientFactory httpClientFactory,
        ILogger<CategoryService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    private HttpClient CreateClient(string token)
    {
        var client = _httpClientFactory.CreateClient("RaceDayApi");

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        return client;
    }

    public async Task<List<CategoryViewModel>> GetAllAsync(
        string token,
        CancellationToken cancellationToken = default)
    {
        using var client = CreateClient(token);

        using var response = await client.GetAsync(
            "api/Categories", cancellationToken);

        response.EnsureSuccessStatusCode();

        return await response.Content
            .ReadFromJsonAsync<List<CategoryViewModel>>(
                JsonOptions, cancellationToken)
            ?? new List<CategoryViewModel>();
    }

    public async Task<CategoryViewModel?> GetByIdAsync(
        int id,
        string token,
        CancellationToken cancellationToken = default)
    {
        using var client = CreateClient(token);

        using var response = await client.GetAsync(
            $"api/Categories/{id}", cancellationToken);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();

        return await response.Content
            .ReadFromJsonAsync<CategoryViewModel>(
                JsonOptions, cancellationToken);
    }

    public async Task<(bool Success, string Message)> CreateAsync(
        CategoryViewModel model,
        string token,
        CancellationToken cancellationToken = default)
    {
        using var client = CreateClient(token);

        var request = new
        {
            model.Title,
            model.Description
        };

        using var response = await client.PostAsJsonAsync(
            "api/Categories", request, cancellationToken);

        if (response.IsSuccessStatusCode)
            return (true, "Category created successfully.");

        return (false, await GetErrorMessageAsync(response));
    }

    public async Task<(bool Success, string Message)> UpdateAsync(
        int id,
        CategoryViewModel model,
        string token,
        CancellationToken cancellationToken = default)
    {
        using var client = CreateClient(token);

        var request = new
        {
            CategoryId = id,
            model.Title,
            model.Description
        };

        using var response = await client.PutAsJsonAsync(
            $"api/Categories/{id}", request, cancellationToken);

        if (response.IsSuccessStatusCode)
            return (true, "Category updated successfully.");

        return (false, await GetErrorMessageAsync(response));
    }

    public async Task<(bool Success, string Message)> DeleteAsync(
        int id,
        string token,
        CancellationToken cancellationToken = default)
    {
        using var client = CreateClient(token);

        using var response = await client.DeleteAsync(
            $"api/Categories/{id}", cancellationToken);

        if (response.IsSuccessStatusCode)
            return (true, "Category deleted successfully.");

        return (false, await GetErrorMessageAsync(response));
    }

    private async Task<string> GetErrorMessageAsync(
        HttpResponseMessage response)
    {
        var status = response.StatusCode;

        try
        {
            var body = await response.Content.ReadAsStringAsync();

            if (!string.IsNullOrWhiteSpace(body))
            {
                using var document = JsonDocument.Parse(body);
                var root = document.RootElement;

                foreach (var key in new[] { "message", "detail", "title" })
                {
                    if (root.TryGetProperty(key, out var value) &&
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
            // Return a safe message if the API response isn't JSON.
        }

        _logger.LogWarning(
            "Category API request failed with status {StatusCode}.", status);

        return status switch
        {
            System.Net.HttpStatusCode.Unauthorized =>
                "Your session has expired. Please sign in again.",

            System.Net.HttpStatusCode.Forbidden =>
                "You do not have permission to perform this action.",

            System.Net.HttpStatusCode.NotFound =>
                "The requested category could not be found.",

            System.Net.HttpStatusCode.Conflict =>
                "The category cannot be changed because it conflicts with existing data.",

            System.Net.HttpStatusCode.BadRequest =>
                "The category details are invalid. Please check your entries.",

            _ => "The operation could not be completed. Please try again."
        };
    }
}