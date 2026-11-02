
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace race_day_backend_api.Services
{
    public interface IBlobStorageService
    {
        Task<string> UploadFileAsync(
            IFormFile file,
            string? folder = null);

        Task DeleteFileAsync(string blobName);
    }

    public class ImageStorageService : IBlobStorageService
    {
        private readonly BlobContainerClient _containerClient;
        private readonly ILogger<ImageStorageService> _logger;

        public ImageStorageService(
            IConfiguration configuration,
            ILogger<ImageStorageService> logger)
        {
            _logger = logger;

            var connectionString =
                configuration["AzureClients:AzureConnectionString"];

            var containerName =
                configuration["AzureClients:ContainerName"]
                ?? "racedayimages";

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    "Azure Blob Storage connection string is missing.");
            }

            var blobServiceClient =
                new BlobServiceClient(connectionString);

            _containerClient =
                blobServiceClient.GetBlobContainerClient(containerName);
        }

        public async Task<string> UploadFileAsync(
            IFormFile file,
            string? folder = null)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("No file provided.");

            try
            {
                // Create a private container if it does not exist.
                await _containerClient.CreateIfNotExistsAsync();

                var extension =
                    Path.GetExtension(file.FileName).ToLowerInvariant();

                var fileName = $"{Guid.NewGuid():N}{extension}";

                var blobName = string.IsNullOrWhiteSpace(folder)
                    ? fileName
                    : $"{folder.Trim('/')}/{fileName}";

                var blobClient =
                    _containerClient.GetBlobClient(blobName);

                await using var stream = file.OpenReadStream();

                var uploadOptions = new BlobUploadOptions
                {
                    HttpHeaders = new BlobHttpHeaders
                    {
                        ContentType = file.ContentType
                    }
                };

                await blobClient.UploadAsync(
                    stream,
                    uploadOptions);

                _logger.LogInformation(
                    "Image uploaded successfully: {BlobName}",
                    blobName);

                return blobClient.Uri.ToString();
            }
            catch (RequestFailedException ex)
            {
                _logger.LogError(
                    ex,
                    "Azure Blob Storage error. Status: {Status}, Code: {ErrorCode}",
                    ex.Status,
                    ex.ErrorCode);

                throw;
            }
        }

        public async Task DeleteFileAsync(string blobName)
        {
            if (string.IsNullOrWhiteSpace(blobName))
                throw new ArgumentException("Blob name is required.");

            try
            {
                var blobClient =
                    _containerClient.GetBlobClient(blobName);

                await blobClient.DeleteIfExistsAsync();
            }
            catch (RequestFailedException ex)
            {
                _logger.LogError(
                    ex,
                    "Could not delete Azure blob: {BlobName}",
                    blobName);

                throw;
            }
        }
    }
}
