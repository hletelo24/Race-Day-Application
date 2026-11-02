using System.Text.Json;
using front_end_web.Models.Events;
using front_end_web.Models.Organiser;
using front_end_web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace front_end_web.Controllers;

public class OrganiserController : Controller
{
    private readonly IOrganiserService _organiserService;
    private readonly ICategoryService _categoryService;
    private readonly IEventService _eventService;
    private readonly ILogger<OrganiserController> _logger;

    public OrganiserController(
        IOrganiserService organiserService,
        ICategoryService categoryService,
        IEventService eventService,
        ILogger<OrganiserController> logger)
    {
        _organiserService = organiserService;
        _categoryService = categoryService;
        _eventService = eventService;
        _logger = logger;
    }

    // ---------- Shared access/session helpers ----------

    private string? Token => HttpContext.Session.GetString("JwtToken");

    private bool IsOrganiser()
    {
        return !string.IsNullOrWhiteSpace(Token)
            && !string.IsNullOrWhiteSpace(
                HttpContext.Session.GetString("UserId"))
            && string.Equals(
                HttpContext.Session.GetString("UserRole"),
                "Organiser",
                StringComparison.OrdinalIgnoreCase);
    }

    private IActionResult? CheckAccess()
    {
        if (string.IsNullOrWhiteSpace(Token) ||
            string.IsNullOrWhiteSpace(HttpContext.Session.GetString("UserId")))
        {
            return RedirectToAction(
                "Login", "Auth",
                new { returnUrl = Request.Path.ToString() });
        }

        if (!IsOrganiser())
            return RedirectToAction("AccessDenied", "Auth");

        return null;
    }

    private async Task LoadEventOptionsAsync(
        int? selectedCategoryId = null,
        int? selectedRouteId = null,
        CancellationToken cancellationToken = default)
    {
        var categories = await _eventService.GetCategoriesAsync(
            Token!, cancellationToken);
        var routes = await _eventService.GetRoutesAsync(
            Token!, cancellationToken);

        ViewBag.Categories = new SelectList(
            categories, "CategoryId", "Title", selectedCategoryId);
        ViewBag.Routes = new SelectList(
            routes, "RouteId", "Title", selectedRouteId);
    }

    // ---------- Category management ----------

    [HttpGet]
    public async Task<IActionResult> Categories(
        CancellationToken cancellationToken)
    {
        var access = CheckAccess();
        if (access != null) return access;

        try
        {
            var categories = await _categoryService.GetAllAsync(
                Token!, cancellationToken);

            return View(categories);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to retrieve categories.");
            TempData["ErrorMessage"] =
                "Unable to load categories. Please try again.";
            return View(new List<CategoryViewModel>());
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Invalid category response.");
            TempData["ErrorMessage"] =
                "The category data could not be read.";
            return View(new List<CategoryViewModel>());
        }
    }

    [HttpGet]
    public IActionResult CreateCategory()
    {
        var access = CheckAccess();
        if (access != null) return access;

        return View(new CategoryViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateCategory(
        CategoryViewModel model,
        CancellationToken cancellationToken)
    {
        var access = CheckAccess();
        if (access != null) return access;

        if (!ModelState.IsValid)
            return View(model);

        try
        {
            var result = await _categoryService.CreateAsync(
                model, Token!, cancellationToken);

            if (!result.Success)
            {
                ModelState.AddModelError(string.Empty, result.Message);
                return View(model);
            }

            TempData["SuccessMessage"] = result.Message;
            return RedirectToAction(nameof(Categories));
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to create category.");
            ModelState.AddModelError(
                string.Empty,
                "The category could not be created. Please try again.");
            return View(model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> EditCategory(
        int id,
        CancellationToken cancellationToken)
    {
        var access = CheckAccess();
        if (access != null) return access;
        if (id <= 0) return BadRequest();

        try
        {
            var category = await _categoryService.GetByIdAsync(
                id, Token!, cancellationToken);

            if (category == null) return NotFound();
            return View(category);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex,
                "Failed to retrieve category {CategoryId}.", id);
            TempData["ErrorMessage"] = "Unable to retrieve the category.";
            return RedirectToAction(nameof(Categories));
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditCategory(
        int id,
        CategoryViewModel model,
        CancellationToken cancellationToken)
    {
        var access = CheckAccess();
        if (access != null) return access;

        if (id <= 0 || id != model.CategoryId)
            return BadRequest();

        if (!ModelState.IsValid)
            return View(model);

        try
        {
            var result = await _categoryService.UpdateAsync(
                id, model, Token!, cancellationToken);

            if (!result.Success)
            {
                ModelState.AddModelError(string.Empty, result.Message);
                return View(model);
            }

            TempData["SuccessMessage"] = result.Message;
            return RedirectToAction(nameof(Categories));
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex,
                "Failed to update category {CategoryId}.", id);
            ModelState.AddModelError(
                string.Empty,
                "The category could not be updated. Please try again.");
            return View(model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> DeleteCategory(
        int id,
        CancellationToken cancellationToken)
    {
        var access = CheckAccess();
        if (access != null) return access;
        if (id <= 0) return BadRequest();

        try
        {
            var category = await _categoryService.GetByIdAsync(
                id, Token!, cancellationToken);

            if (category == null) return NotFound();
            return View(category);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex,
                "Failed to retrieve category {CategoryId}.", id);
            TempData["ErrorMessage"] = "Unable to retrieve the category.";
            return RedirectToAction(nameof(Categories));
        }
    }

    [HttpPost]
    [ActionName("DeleteCategory")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteCategoryConfirmed(
        int id,
        CancellationToken cancellationToken)
    {
        var access = CheckAccess();
        if (access != null) return access;
        if (id <= 0) return BadRequest();

        try
        {
            var result = await _categoryService.DeleteAsync(
                id, Token!, cancellationToken);

            TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] =
                result.Message;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex,
                "Failed to delete category {CategoryId}.", id);
            TempData["ErrorMessage"] =
                "The category could not be deleted. It may be linked to events.";
        }

        return RedirectToAction(nameof(Categories));
    }

    // ---------- Organiser dashboard ----------

    [HttpGet]
    public async Task<IActionResult> Dashboard(
        CancellationToken cancellationToken)
    {
        var access = CheckAccess();
        if (access != null) return access;

        var model = new OrganiserDashboardViewModel
        {
            OrganiserName =
                HttpContext.Session.GetString("UserName") ?? "Organiser"
        };

        try
        {
            var events = await _organiserService.GetEventsAsync(
                Token!, cancellationToken);

            var enrolments = await _organiserService.GetEnrolmentsAsync(
                Token!, cancellationToken);

            var today = DateTime.Today;

            model.Events = events
                .Where(item => item.Date >= today)
                .OrderBy(item => item.Date)
                .Take(5)
                .ToList();

            model.TotalEvents = events.Count;
            model.UpcomingEvents = events.Count(item => item.Date >= today);
            model.TotalEnrolments = enrolments.Count;

            foreach (var item in model.Events)
            {
                item.EnrolmentCount = enrolments.Count(
                    enrolment => enrolment.EventId == item.EventId);
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to load organiser dashboard.");
            model.ErrorMessage =
                "The RaceDay API is unavailable. Please try again shortly.";
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Invalid dashboard response.");
            model.ErrorMessage =
                "The server returned data in an unexpected format.";
        }

        return View(model);
    }

    // ---------- Event CRUD ----------

    [HttpGet]
    public async Task<IActionResult> Events(
        CancellationToken cancellationToken)
    {
        var access = CheckAccess();
        if (access != null) return access;

        try
        {
            var events = await _eventService.GetEventsAsync(Token!, cancellationToken);
            return View(events.OrderBy(item => item.Date).ToList());
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to load events.");
            TempData["ErrorMessage"] = "Unable to load events. Please try again.";
            return View(new List<EventViewModel>());
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Invalid events response.");
            TempData["ErrorMessage"] = "The event data could not be read.";
            return View(new List<EventViewModel>());
        }
    }

    [HttpGet]
    public async Task<IActionResult> CreateEvent(
        CancellationToken cancellationToken)
    {
        var access = CheckAccess();
        if (access != null) return access;

        try
        {
            await LoadEventOptionsAsync(null, null, cancellationToken);
            return View(new EventViewModel());
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to load category/route options.");
            TempData["ErrorMessage"] = "Categories and routes could not be loaded.";
            return RedirectToAction(nameof(Events));
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateEvent(
        EventViewModel model, CancellationToken cancellationToken)
    {
        var access = CheckAccess();
        if (access != null) return access;

        if (!ModelState.IsValid)
        {
            await LoadEventOptionsAsync(model.CategoryId, model.RouteId, cancellationToken);
            return View(model);
        }

        try
        {
            var result = await _eventService.CreateEventAsync(model, Token!, cancellationToken);
            if (!result.Success)
            {
                ModelState.AddModelError(string.Empty, result.Message);
                await LoadEventOptionsAsync(model.CategoryId, model.RouteId, cancellationToken);
                return View(model);
            }

            TempData["SuccessMessage"] = result.Message;
            return RedirectToAction(nameof(Events));
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to create event.");
            ModelState.AddModelError(string.Empty, "The event could not be created.");
            await LoadEventOptionsAsync(model.CategoryId, model.RouteId, cancellationToken);
            return View(model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> EditEvent(
        int id, CancellationToken cancellationToken)
    {
        var access = CheckAccess();
        if (access != null) return access;
        if (id <= 0) return BadRequest();

        try
        {
            var model = await _eventService.GetEventAsync(id, Token!, cancellationToken);
            if (model == null) return NotFound();

            await LoadEventOptionsAsync(model.CategoryId, model.RouteId, cancellationToken);
            return View(model);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to load event {EventId}.", id);
            TempData["ErrorMessage"] = "The event could not be loaded.";
            return RedirectToAction(nameof(Events));
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditEvent(
        int id, EventViewModel model, CancellationToken cancellationToken)
    {
        var access = CheckAccess();
        if (access != null) return access;
        if (id <= 0 || id != model.EventId) return BadRequest();

        if (!ModelState.IsValid)
        {
            await LoadEventOptionsAsync(model.CategoryId, model.RouteId, cancellationToken);
            return View(model);
        }

        try
        {
            var result = await _eventService.UpdateEventAsync(id, model, Token!, cancellationToken);
            if (!result.Success)
            {
                ModelState.AddModelError(string.Empty, result.Message);
                await LoadEventOptionsAsync(model.CategoryId, model.RouteId, cancellationToken);
                return View(model);
            }

            TempData["SuccessMessage"] = result.Message;
            return RedirectToAction(nameof(Events));
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to update event {EventId}.", id);
            ModelState.AddModelError(string.Empty, "The event could not be updated.");
            await LoadEventOptionsAsync(model.CategoryId, model.RouteId, cancellationToken);
            return View(model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> DeleteEvent(
        int id, CancellationToken cancellationToken)
    {
        var access = CheckAccess();
        if (access != null) return access;
        if (id <= 0) return BadRequest();

        try
        {
            var model = await _eventService.GetEventAsync(id, Token!, cancellationToken);
            if (model == null) return NotFound();
            return View(model);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to load event {EventId} for deletion.", id);
            TempData["ErrorMessage"] = "The event could not be loaded.";
            return RedirectToAction(nameof(Events));
        }
    }

    [HttpPost]
    [ActionName("DeleteEvent")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteEventConfirmed(
        int id, CancellationToken cancellationToken)
    {
        var access = CheckAccess();
        if (access != null) return access;
        if (id <= 0) return BadRequest();

        try
        {
            var result = await _eventService.DeleteEventAsync(id, Token!, cancellationToken);
            TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to delete event {EventId}.", id);
            TempData["ErrorMessage"] =
                "The event could not be deleted. It may have existing enrolments.";
        }

        return RedirectToAction(nameof(Events));
    }

    // ---------- Enrolments and results ----------

    [HttpGet]
    public async Task<IActionResult> Enrolments(
    CancellationToken cancellationToken)
    {
        var access = CheckAccess();
        if (access != null) return access;

        try
        {
            var enrolments = await _organiserService.GetEnrolmentsAsync(
                Token!, cancellationToken);

            // Cache event names so the same event is not requested repeatedly.
            var eventNames = new Dictionary<int, string>();

            foreach (var enrolment in enrolments)
            {
                if (!eventNames.TryGetValue(
                        enrolment.EventId, out var eventName))
                {
                    var eventDetails = await _eventService.GetEventAsync(
                        enrolment.EventId,
                        Token!,
                        cancellationToken);
                    
                    eventName = eventDetails?.Title ?? "Unknown event";
                    eventNames[enrolment.EventId] = eventName;
                    
                }
                enrolment.EventName = eventName;
            }

            return View(enrolments);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to load enrolments.");

            TempData["ErrorMessage"] =
                "Unable to load enrolments. Please try again.";

            return View(new List<OrganiserEnrolmentViewModel>());
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Invalid enrolments response.");

            TempData["ErrorMessage"] =
                "The enrolment data could not be read.";

            return View(new List<OrganiserEnrolmentViewModel>());
        }
    }

    [HttpGet]
    public IActionResult Results()
    {
        var access = CheckAccess();
        if (access != null) return access;

        // Result entry/publishing actions should be connected to API endpoints.
        return View(new List<OrganiserResultViewModel>());
    }
}
