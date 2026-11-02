
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using front_end_web.Models.Profile;

namespace front_end_web.Services
{
    public class ProfileService : IProfileService
    {
        private readonly IHttpClientFactory _factory;

        private readonly JsonSerializerOptions _jsonOptions =
            new(JsonSerializerDefaults.Web);

        public ProfileService(IHttpClientFactory factory)
        {
            _factory = factory;
        }

        private HttpClient CreateClient(string token)
        {
            var client = _factory.CreateClient("RaceDayApi");

            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            return client;
        }

        public async Task<ProfileViewModel?> GetMyProfileAsync(
            string token,
            CancellationToken ct = default)
        {
            using var client = CreateClient(token);

            using var response = await client.GetAsync(
                "api/profile/me", ct);

            if (response.StatusCode ==
                System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }

            response.EnsureSuccessStatusCode();

            return await response.Content
                .ReadFromJsonAsync<ProfileViewModel>(
                    _jsonOptions, ct);
        }

        public async Task<(bool Success, string Message, string? ImageUrl)>
            UploadProfileImageAsync(
                IFormFile image,
                string token,
                CancellationToken ct = default)
        {
            using var client = CreateClient(token);

            // STEP 1: Upload image to Azure using ImageUploadController.
            using var form = new MultipartFormDataContent();

            await using var stream = image.OpenReadStream();

            using var fileContent = new StreamContent(stream);

            fileContent.Headers.ContentType =
                new MediaTypeHeaderValue(
                    string.IsNullOrWhiteSpace(image.ContentType)
                        ? "application/octet-stream"
                        : image.ContentType);

            // The API upload model has an Image property.
            form.Add(
                fileContent,
                "Image",
                Path.GetFileName(image.FileName));

            using var uploadResponse = await client.PostAsync(
                "api/ImageUpload/upload?folder=profiles",
                form,
                ct);

            var uploadBody =
                await uploadResponse.Content.ReadAsStringAsync(ct);

            if (!uploadResponse.IsSuccessStatusCode)
            {
                return (
                    false,
                    $"Azure image upload failed (HTTP {(int)uploadResponse.StatusCode}): {uploadBody}",
                    null
                );
            }

            string? imageUrl;

            try
            {
                using var json = JsonDocument.Parse(uploadBody);

                imageUrl = json.RootElement
                    .TryGetProperty("imageUrl", out var imageElement)
                    ? imageElement.GetString()
                    : null;
            }
            catch (JsonException)
            {
                return (
                    false,
                    "The image upload API returned an invalid response.",
                    null
                );
            }

            if (string.IsNullOrWhiteSpace(imageUrl))
            {
                return (
                    false,
                    "The image was uploaded, but no image URL was returned.",
                    null
                );
            }

            // STEP 2: Save the Azure URL to the user's profile.
            var updateRequest = new
            {
                ImageUrl = imageUrl
            };

            using var updateResponse = await client.PutAsJsonAsync(
                "api/Profile/me/profile-image",
                updateRequest,
                _jsonOptions,
                ct);

            var updateBody =
                await updateResponse.Content.ReadAsStringAsync(ct);

            if (!updateResponse.IsSuccessStatusCode)
            {
                return (
                    false,
                    $"Image uploaded, but profile update failed (HTTP {(int)updateResponse.StatusCode}): {updateBody}",
                    imageUrl
                );
            }

            return (
                true,
                "Your profile picture has been updated successfully.",
                imageUrl
            );
        }
    }
}
