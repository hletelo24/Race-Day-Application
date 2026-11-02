
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using race_day_backend_api.Data;
using race_day_backend_api.Models;

namespace race_day_backend_api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Participant,Organiser")]
    public class ProfileController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        public ProfileController(
            ApplicationDbContext context,
            IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        // GET: api/Profile/me
        [HttpGet("me")]
        public async Task<IActionResult> GetMyProfile()
        {
            var userId = GetCurrentUserId();

            if (userId == null)
                return Unauthorized("Unable to identify the current user.");

            var user = await _context.UserAccounts
                .FirstOrDefaultAsync(u => u.UserId == userId.Value);

            if (user == null)
                return NotFound("User profile not found.");

            return Ok(new
            {
                user.UserId,
                user.FirstName,
                user.LastName,
                user.EmailAddress,
                user.ImageUrl
            });
        }

        // PUT: api/Profile/me/profile-image

        [HttpPut("me/profile-image")]
        public async Task<IActionResult> UpdateProfileImage(
            [FromBody] UpdateProfileImageRequest request)
        {
            if (request == null ||
                string.IsNullOrWhiteSpace(request.ImageUrl))
            {
                return BadRequest("Image URL is required.");
            }

            var userIdClaim =
                User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ??
                User.FindFirst("sub")?.Value ??
                User.FindFirst("UserId")?.Value;

            if (!int.TryParse(userIdClaim, out var userId))
                return Unauthorized("Invalid user ID.");

            var user = await _context.UserAccounts
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null)
                return NotFound("User profile not found.");

            // Validate the Azure image URL.
            if (!Uri.TryCreate(
                request.ImageUrl,
                UriKind.Absolute,
                out var imageUri))
            {
                return BadRequest("Invalid image URL.");
            }

            var azureBaseUrl =
                _configuration["AzureClients:AzureBlobBaseUrl"];

            var containerName =
                _configuration["AzureClients:ContainerName"];

            if (!Uri.TryCreate(
                azureBaseUrl,
                UriKind.Absolute,
                out var storageUri))
            {
                return StatusCode(500,
                    "Azure Blob Storage URL is not configured correctly.");
            }

            if (string.IsNullOrWhiteSpace(containerName))
            {
                return StatusCode(500,
                    "Azure Blob Storage container name is missing.");
            }

            // Ensure the URL belongs to the configured storage account.
            if (!string.Equals(
                imageUri.Scheme,
                Uri.UriSchemeHttps,
                StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(
                    imageUri.Host,
                    storageUri.Host,
                    StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(
                    "The image URL does not belong to the configured Azure storage account.");
            }

            // Ensure the image belongs to the correct container.
            var expectedContainerPath =
                "/" + containerName.Trim('/') + "/";

            if (!imageUri.AbsolutePath.StartsWith(
                expectedContainerPath,
                StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(
                    "The image URL does not belong to the configured Azure storage container.");
            }

            // Update the user's profile.
            user.ImageUrl = request.ImageUrl;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Profile picture updated successfully.",
                imageUrl = user.ImageUrl
            });
        }


        private int? GetCurrentUserId()
        {
            var userIdClaim =
                User.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
                User.FindFirst("sub")?.Value ??
                User.FindFirst("UserId")?.Value;

            if (int.TryParse(userIdClaim, out var userId))
                return userId;

            return null;
        }
    }

    public class UpdateProfileImageRequest
    {
        public string ImageUrl { get; set; } = string.Empty;
    }
}
