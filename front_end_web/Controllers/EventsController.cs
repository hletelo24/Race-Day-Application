using front_end_web.Models.Events;
using front_end_web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace front_end_web.Controllers
{
    public class EventsController : Controller
    {
        private readonly IEventService _eventService;
        private readonly ILogger<EventsController> _logger;
        private const long MaxBannerSize = 5 * 1024 * 1024;
        private static readonly string[] AllowedExtensions =
            { ".jpg", ".jpeg", ".png", ".webp" };

        private string? Token => HttpContext.Session.GetString("JwtToken");
        private string? Role => HttpContext.Session.GetString("UserRole");

        public EventsController(IEventService eventService, ILogger<EventsController> logger)
        {
            _eventService = eventService;
            _logger = logger;
        }

        private IActionResult? CheckRole(string requiredRole)
        {
            if (string.IsNullOrWhiteSpace(Token))
                return RedirectToAction("Login", "Auth");
            if (!string.Equals(Role, requiredRole, StringComparison.OrdinalIgnoreCase))
                return RedirectToAction("AccessDenied", "Auth");
            return null;
        }

        private async Task PopulateDropdowns(
            int? categoryId = null, int? routeId = null, CancellationToken ct = default)
        {
            var categories = await _eventService.GetCategoriesAsync(Token!, ct);
            var routes = await _eventService.GetRoutesAsync(Token!, ct);
            ViewBag.Categories = new SelectList(categories, "CategoryId", "Title", categoryId);
            // Change Name if your RouteOptionViewModel uses a different display property.
            ViewBag.Routes = new SelectList(routes, "RouteId", "Name", routeId);
        }

        private void ValidateBanner(EventViewModel model)
        {
            var file = model.BannerImage;
            if (file == null || file.Length == 0) return;

            if (file.Length > MaxBannerSize)
                ModelState.AddModelError(nameof(model.BannerImage), "Banner must be 5 MB or smaller.");

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(ext))
                ModelState.AddModelError(nameof(model.BannerImage), "Only JPG, JPEG, PNG and WEBP files are allowed.");
        }

        [HttpGet]
        public async Task<IActionResult> Index(int? categoryId, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(Token))
                return RedirectToAction("Login", "Auth");

            try
            {
                var events = await _eventService.GetEventsAsync(Token!, ct);
                var categories = await _eventService.GetCategoriesAsync(Token!, ct);
                ViewBag.Categories = new SelectList(categories, "CategoryId", "Title", categoryId);
                ViewBag.SelectedCategoryId = categoryId;
                if (categoryId.HasValue)
                    events = events.Where(e => e.CategoryId == categoryId.Value).ToList();
                return View(events.OrderBy(e => e.Date).ToList());
            }
            catch (Exception ex) when (ex is HttpRequestException or System.Text.Json.JsonException)
            {
                _logger.LogError(ex, "Unable to load events.");
                TempData["Error"] = "Events could not be loaded. Please try again.";
                ViewBag.Categories = new SelectList(Array.Empty<CategoryOptionViewModel>(), "CategoryId", "Title");
                return View(new List<EventViewModel>());
            }
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(Token)) return RedirectToAction("Login", "Auth");
            if (id <= 0) return BadRequest();
            try
            {
                var model = await _eventService.GetEventAsync(id, Token!, ct);
                return model == null ? NotFound() : View(model);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Unable to load event {EventId}.", id);
                TempData["Error"] = "The event could not be loaded.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpGet]
        public async Task<IActionResult> CreateEvent(CancellationToken ct)
        {
            var access = CheckRole("Organiser");
            if (access != null) return access;
            await PopulateDropdowns(ct: ct);
            return View(new EventViewModel { Date = DateTime.Today.AddDays(1) });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(6 * 1024 * 1024)]
        [RequestFormLimits(MultipartBodyLengthLimit = 6 * 1024 * 1024)]
        public async Task<IActionResult> CreateEvent(EventViewModel model, CancellationToken ct)
        {
            var access = CheckRole("Organiser");
            if (access != null) return access;
            ValidateBanner(model);

            if (!ModelState.IsValid)
            {
                await PopulateDropdowns(model.CategoryId, model.RouteId, ct);
                return View(model);
            }

            try
            {
                var result = await _eventService.CreateEventAsync(model, Token!, ct);
                TempData[result.Success ? "Success" : "Error"] = result.Message;
                if (result.Success) return RedirectToAction(nameof(Index));
                await PopulateDropdowns(model.CategoryId, model.RouteId, ct);
                return View(model);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Event creation failed.");
                TempData["Error"] = ex.Message;
                await PopulateDropdowns(model.CategoryId, model.RouteId, ct);
                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> EditEvent(int id, CancellationToken ct)
        {
            var access = CheckRole("Organiser");
            if (access != null) return access;
            var model = await _eventService.GetEventAsync(id, Token!, ct);
            if (model == null) return NotFound();
            await PopulateDropdowns(model.CategoryId, model.RouteId, ct);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(6 * 1024 * 1024)]
        [RequestFormLimits(MultipartBodyLengthLimit = 6 * 1024 * 1024)]
        public async Task<IActionResult> EditEvent(int id, EventViewModel model, CancellationToken ct)
        {
            var access = CheckRole("Organiser");
            if (access != null) return access;
            if (id != model.EventId) return BadRequest();
            ValidateBanner(model);

            if (!ModelState.IsValid)
            {
                await PopulateDropdowns(model.CategoryId, model.RouteId, ct);
                return View(model);
            }

            try
            {
                var result = await _eventService.UpdateEventAsync(id, model, Token!, ct);
                TempData[result.Success ? "Success" : "Error"] = result.Message;
                if (result.Success) return RedirectToAction(nameof(Index));
                await PopulateDropdowns(model.CategoryId, model.RouteId, ct);
                return View(model);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Event update failed for {EventId}.", id);
                TempData["Error"] = ex.Message;
                await PopulateDropdowns(model.CategoryId, model.RouteId, ct);
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteEvent(int id, CancellationToken ct)
        {
            var access = CheckRole("Organiser");
            if (access != null) return access;
            try
            {
                var result = await _eventService.DeleteEventAsync(id, Token!, ct);
                TempData[result.Success ? "Success" : "Error"] = result.Message;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Event deletion failed.");
                TempData["Error"] = ex.Message;
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Enrol(EnrolEventViewModel model, CancellationToken ct)
        {
            var access = CheckRole("Participant");
            if (access != null) return access;
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Please select a valid event.";
                return RedirectToAction(nameof(Details), new { id = model.EventId });
            }
            try
            {
                var result = await _eventService.EnrolAsync(model, Token!, ct);
                TempData[result.Success ? "Success" : "Error"] = result.Message;
                return RedirectToAction(result.Success ? nameof(MyEnrolments) : nameof(Details),
                    result.Success ? null : new { id = model.EventId });
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Enrolment failed.");
                TempData["Error"] = "Enrolment failed. You may already be enrolled.";
                return RedirectToAction(nameof(Details), new { id = model.EventId });
            }
        }

        [HttpGet]
        public async Task<IActionResult> MyEvents(CancellationToken ct)
        {
            var access = CheckRole("Participant");
            if (access != null) return access;
            try
            {
                var rows = await _eventService.GetMyEnrolmentsAsync(Token!, ct);
                var ordered = rows
                    .OrderBy(x => !(x.EventDate.HasValue && x.EventDate.Value.Date >= DateTime.Today))
                    .ThenBy(x => x.EventDate ?? DateTime.MaxValue)
                    .ThenBy(x => x.StartTime).ToList();
                return View(ordered);
            }
            catch (Exception ex) when (ex is HttpRequestException or System.Text.Json.JsonException)
            {
                _logger.LogError(ex, "Unable to load participant events.");
                TempData["Error"] = "Your events could not be loaded.";
                return View(new List<ParticipantEnrolmentViewModel>());
            }
        }

        [HttpGet]
        public async Task<IActionResult> MyEnrolments(CancellationToken ct)
        {
            var access = CheckRole("Participant");
            if (access != null) return access;
            try
            {
                return View(await _eventService.GetMyEnrolmentsAsync(Token!, ct));
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Unable to load participant enrolments.");
                TempData["Error"] = "Your enrolments could not be loaded.";
                return View(new List<ParticipantEnrolmentViewModel>());
            }
        }
    }
}
