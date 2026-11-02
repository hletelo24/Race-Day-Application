
using Microsoft.AspNetCore.Mvc;
using race_day_backend_api.Models;
using race_day_backend_api.Services;

namespace race_day_backend_api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ImageUploadController : ControllerBase
    {
        private readonly IBlobStorageService _blobStorageService;
        private readonly ILogger<ImageUploadController> _logger;

        private const long MaxFileSize = 5 * 1024 * 1024;

        public ImageUploadController(
            IBlobStorageService blobStorageService,
            ILogger<ImageUploadController> logger)
        {
            _blobStorageService = blobStorageService;
            _logger = logger;
        }

        [HttpPost("upload")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UploadImage(
            [FromForm] ImageUploadRequest request,
            [FromQuery] string folder = "general")
        {
            var image = request?.Image;

            if (image == null || image.Length == 0)
                return BadRequest("Please select an image.");

            if (image.Length > MaxFileSize)
                return BadRequest("Maximum image size is 5 MB.");

            var extension =
                Path.GetExtension(image.FileName).ToLowerInvariant();

            var allowedExtensions = new[]
            {
                ".jpg", ".jpeg", ".png", ".webp"
            };

            if (!allowedExtensions.Contains(extension))
                return BadRequest("Unsupported image format.");

            var allowedContentTypes = new[]
            {
                "image/jpeg",
                "image/png",
                "image/webp"
            };

            if (!allowedContentTypes.Contains(
                image.ContentType.ToLowerInvariant()))
            {
                return BadRequest("Invalid image content type.");
            }

            try
            {
                var imageUrl =
                    await _blobStorageService.UploadFileAsync(
                        image,
                        folder);

                return Ok(new
                {
                    message = "Image uploaded successfully.",
                    imageUrl
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Image upload failed.");

                return StatusCode(500, new
                {
                    message = "Image upload failed. Please try again."
                });
            }
        }
    }
}
