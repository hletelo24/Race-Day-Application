using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using front_end_web.Models.Events;

namespace front_end_web.Services
{
    public class EventService : IEventService
    {
        private readonly IHttpClientFactory _httpClientFactory;

        private static readonly JsonSerializerOptions JsonOptions =
            new(JsonSerializerDefaults.Web) { PropertyNameCaseInsensitive = true };

        public EventService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        private HttpClient CreateClient(string token)
        {
            var client = _httpClientFactory.CreateClient("RaceDayApi");
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
            return client;
        }

        private static async Task<string> ErrorMessageAsync(
            HttpResponseMessage response, CancellationToken ct = default)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            if (string.IsNullOrWhiteSpace(body))
                return $"API request failed ({(int)response.StatusCode}).";

            try
            {
                using var doc = JsonDocument.Parse(body);
                foreach (var key in new[] { "message", "detail", "title" })
                    if (doc.RootElement.TryGetProperty(key, out var value) &&
                        value.ValueKind == JsonValueKind.String)
                        return value.GetString()!;
            }
            catch (JsonException) { }

            return body.Length > 500 ? body[..500] : body;
        }

        private static async Task EnsureSuccessAsync(
            HttpResponseMessage response, CancellationToken ct = default)
        {
            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException(
                    await ErrorMessageAsync(response, ct),
                    null, response.StatusCode);
        }

        private sealed class ImageUploadResponse
        {
            public string? ImageUrl { get; set; }
            public string? Message { get; set; }
        }

        private async Task<(bool Success, string? ImageUrl, string Message)>
            UploadEventBannerAsync(
                IFormFile banner, string token, CancellationToken ct)
        {
            if (banner == null || banner.Length == 0)
                return (false, null, "Please select a banner image.");

            using var client = CreateClient(token);
            using var form = new MultipartFormDataContent();
            await using var stream = banner.OpenReadStream();
            using var fileContent = new StreamContent(stream);

            fileContent.Headers.ContentType = new MediaTypeHeaderValue(
                string.IsNullOrWhiteSpace(banner.ContentType)
                    ? "application/octet-stream" : banner.ContentType);

            // Matches ImageUploadRequest.Image in the API.
            form.Add(fileContent, "Image", Path.GetFileName(banner.FileName));

            using var response = await client.PostAsync(
                "api/ImageUpload/upload?folder=events", form, ct);

            if (!response.IsSuccessStatusCode)
                return (false, null, await ErrorMessageAsync(response, ct));

            var result = await response.Content.ReadFromJsonAsync<ImageUploadResponse>(
                JsonOptions, ct);

            if (string.IsNullOrWhiteSpace(result?.ImageUrl))
                return (false, null, "The image API did not return imageUrl.");

            return (true, result.ImageUrl, result.Message ?? "Banner uploaded.");
        }

        private static object ToEventPayload(EventViewModel model) => new
        {
            model.EventId,
            model.CategoryId,
            model.RouteId,
            model.Title,
            model.Description,
            model.StartLocation,
            model.EndLocation,
            model.StartTime,
            model.Distance,
            model.Date,
            model.ImageUrl
        };

        public async Task<List<EventViewModel>> GetEventsAsync(
            string token, CancellationToken cancellationToken = default)
        {
            using var client = CreateClient(token);
            using var response = await client.GetAsync("api/events", cancellationToken);
            await EnsureSuccessAsync(response, cancellationToken);
            return await response.Content.ReadFromJsonAsync<List<EventViewModel>>(
                JsonOptions, cancellationToken) ?? [];
        }

        public async Task<EventViewModel?> GetEventAsync(
            int id, string token, CancellationToken cancellationToken = default)
        {
            using var client = CreateClient(token);
            using var response = await client.GetAsync($"api/events/{id}", cancellationToken);
            if (response.StatusCode == HttpStatusCode.NotFound) return null;
            await EnsureSuccessAsync(response, cancellationToken);
            return await response.Content.ReadFromJsonAsync<EventViewModel>(
                JsonOptions, cancellationToken);
        }

        public async Task<List<CategoryOptionViewModel>> GetCategoriesAsync(
            string token, CancellationToken cancellationToken = default)
        {
            using var client = CreateClient(token);
            using var response = await client.GetAsync("api/categories", cancellationToken);
            await EnsureSuccessAsync(response, cancellationToken);
            return await response.Content.ReadFromJsonAsync<List<CategoryOptionViewModel>>(
                JsonOptions, cancellationToken) ?? [];
        }

        public async Task<List<RouteOptionViewModel>> GetRoutesAsync(
            string token, CancellationToken cancellationToken = default)
        {
            using var client = CreateClient(token);
            using var response = await client.GetAsync("api/routes", cancellationToken);
            await EnsureSuccessAsync(response, cancellationToken);
            return await response.Content.ReadFromJsonAsync<List<RouteOptionViewModel>>(
                JsonOptions, cancellationToken) ?? [];
        }

        public async Task<(bool Success, string Message)> CreateEventAsync(
            EventViewModel model, string token, CancellationToken cancellationToken = default)
        {
            if (model.BannerImage is { Length: > 0 })
            {
                var upload = await UploadEventBannerAsync(
                    model.BannerImage, token, cancellationToken);
                if (!upload.Success)
                    return (false, $"Banner upload failed: {upload.Message}");
                model.ImageUrl = upload.ImageUrl;
            }

            using var client = CreateClient(token);
            using var response = await client.PostAsJsonAsync(
                "api/events", ToEventPayload(model), cancellationToken);

            return response.IsSuccessStatusCode
                ? (true, "Event created successfully.")
                : (false, await ErrorMessageAsync(response, cancellationToken));
        }

        public async Task<(bool Success, string Message)> UpdateEventAsync(
            int id, EventViewModel model, string token, CancellationToken cancellationToken = default)
        {
            if (model.BannerImage is { Length: > 0 })
            {
                var upload = await UploadEventBannerAsync(
                    model.BannerImage, token, cancellationToken);
                if (!upload.Success)
                    return (false, $"Banner upload failed: {upload.Message}");
                model.ImageUrl = upload.ImageUrl;
            }

            using var client = CreateClient(token);
            using var response = await client.PutAsJsonAsync(
                $"api/events/{id}", ToEventPayload(model), cancellationToken);

            return response.IsSuccessStatusCode
                ? (true, "Event updated successfully.")
                : (false, await ErrorMessageAsync(response, cancellationToken));
        }

        public async Task<(bool Success, string Message)> DeleteEventAsync(
            int id, string token, CancellationToken cancellationToken = default)
        {
            using var client = CreateClient(token);
            using var response = await client.DeleteAsync($"api/events/{id}", cancellationToken);
            return response.IsSuccessStatusCode
                ? (true, "Event deleted successfully.")
                : (false, await ErrorMessageAsync(response, cancellationToken));
        }

        public async Task<(bool Success, string Message)> EnrolAsync(
            EnrolEventViewModel model, string token, CancellationToken cancellationToken = default)
        {
            using var client = CreateClient(token);
            using var response = await client.PostAsJsonAsync(
                "api/enrolments", new { model.EventId }, cancellationToken);
            return response.IsSuccessStatusCode
                ? (true, "You have successfully enrolled in this event.")
                : (false, await ErrorMessageAsync(response, cancellationToken));
        }

        public async Task<List<ParticipantEnrolmentViewModel>> GetMyEnrolmentsAsync(
            string token, CancellationToken cancellationToken = default)
        {
            using var client = CreateClient(token);
            using var response = await client.GetAsync(
                "api/enrolments/my-enrolments", cancellationToken);
            await EnsureSuccessAsync(response, cancellationToken);

            var rows = await response.Content.ReadFromJsonAsync<List<EnrolmentApiDto>>(
                JsonOptions, cancellationToken) ?? [];

            return rows.Select(x => new ParticipantEnrolmentViewModel
            {
                EnrolmentId = x.EnrolmentId,
                EventId = x.EventId,
                CreatedAt = x.CreatedAt,
                EventTitle = x.Event?.Title ?? $"Event #{x.EventId}",
                EventDate = x.Event?.Date,
                StartTime = x.Event?.StartTime,
                StartLocation = x.Event?.StartLocation,
                EndLocation = x.Event?.EndLocation,
                Distance = x.Event?.Distance,
                CategoryTitle = x.Event?.Category?.Title
            }).ToList();
        }

        private sealed class EnrolmentApiDto
        {
            public int EnrolmentId { get; set; }
            public int EventId { get; set; }
            public int UserId { get; set; }
            public DateTime CreatedAt { get; set; }
            public EventApiDto? Event { get; set; }
        }

        private sealed class EventApiDto
        {
            public int EventId { get; set; }
            public string Title { get; set; } = string.Empty;
            public DateTime Date { get; set; }
            public TimeSpan StartTime { get; set; }
            public string? StartLocation { get; set; }
            public string? EndLocation { get; set; }
            public decimal Distance { get; set; }
            public CategoryApiDto? Category { get; set; }
        }

        private sealed class CategoryApiDto
        {
            public string? Title { get; set; }
        }
    }
}
