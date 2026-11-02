
using front_end_web.Models.Profile;
using front_end_web.Services;
using Microsoft.AspNetCore.Mvc;

namespace front_end_web.Controllers
{
    public class ProfileController : Controller
    {
        private readonly IProfileService _profileService;
        private readonly ILogger<ProfileController> _logger;

        private const long MaxFileSize = 5 * 1024 * 1024;

        private static readonly string[] AllowedExtensions =
        {
            ".jpg", ".jpeg", ".png", ".webp"
        };

        public ProfileController(
            IProfileService profileService,
            ILogger<ProfileController> logger)
        {
            _profileService = profileService;
            _logger = logger;
        }

        private string? Token =>
            HttpContext.Session.GetString("JwtToken");

        private IActionResult? CheckProfileAccess()
        {
            if (string.IsNullOrWhiteSpace(Token))
                return RedirectToAction("Login", "Account");

            var role = HttpContext.Session.GetString("UserRole");

            if (role is not ("Participant" or "Organiser"))
                return Forbid();

            return null;
        }

        [HttpGet]
        public async Task<IActionResult> Index(CancellationToken ct)
        {
            var access = CheckProfileAccess();

            if (access != null)
                return access;

            try
            {
                var model = await _profileService.GetMyProfileAsync(
                    Token!, ct);

                if (model == null)
                    return NotFound();

                return View(model);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(
                    ex, "Could not load current user profile.");

                TempData["ErrorMessage"] =
                    "Unable to load your profile right now.";

                return View(new ProfileViewModel
                {
                    FirstName =
                        HttpContext.Session.GetString("UserName") ?? ""
                });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(6 * 1024 * 1024)]
        [RequestFormLimits(MultipartBodyLengthLimit = 6 * 1024 * 1024)]
        public async Task<IActionResult> UploadImage(
            ProfileViewModel model,
            CancellationToken ct)
        {
            var access = CheckProfileAccess();

            if (access != null)
                return access;

            if (model.Image == null || model.Image.Length == 0)
            {
                TempData["ErrorMessage"] =
                    "Please select a profile picture.";

                return RedirectToAction(nameof(Index));
            }

            if (model.Image.Length > MaxFileSize)
            {
                TempData["ErrorMessage"] =
                    "The image must be 5 MB or smaller.";

                return RedirectToAction(nameof(Index));
            }

            var extension = Path.GetExtension(
                model.Image.FileName).ToLowerInvariant();

            if (!AllowedExtensions.Contains(extension))
            {
                TempData["ErrorMessage"] =
                    "Only JPG, JPEG, PNG and WEBP images are allowed.";

                return RedirectToAction(nameof(Index));
            }

            try
            {
                var result =
                    await _profileService.UploadProfileImageAsync(
                        model.Image,
                        Token!,
                        ct);

                TempData[result.Success
                    ? "SuccessMessage"
                    : "ErrorMessage"] = result.Message;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(
                    ex, "Profile picture API request failed.");

                TempData["ErrorMessage"] =
                    "Unable to communicate with the image upload service.";
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex, "Unexpected profile image upload error.");

                TempData["ErrorMessage"] =
                    "An unexpected error occurred while uploading your picture.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
