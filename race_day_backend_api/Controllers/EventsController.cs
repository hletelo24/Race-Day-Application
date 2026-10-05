using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using race_day_backend_api.Data;
using race_day_backend_api.Models;

namespace race_day_backend_api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class EventsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public EventsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ============================================================
        // GET ALL EVENTS
        // GET: api/events
        // Participant + Organiser
        // ============================================================

        [HttpGet]
        [Authorize(Roles = "Participant,Organiser")]
        public async Task<IActionResult> GetEvents()
        {
            var events = await _context.Events
                .Include(e => e.Category)
                .Include(e => e.Route)
                .AsNoTracking()
                .OrderBy(e => e.Date)
                .ThenBy(e => e.StartTime)
                .ToListAsync();

            return Ok(events);
        }

        // ============================================================
        // GET EVENT BY ID
        // GET: api/events/1
        // Participant + Organiser
        // ============================================================

        [HttpGet("{id:int}")]
        [Authorize(Roles = "Participant,Organiser")]
        public async Task<IActionResult> GetEvent(int id)
        {
            var eventItem = await _context.Events
                .Include(e => e.Category)
                .Include(e => e.Route)
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.EventId == id);

            if (eventItem == null)
            {
                return NotFound(new
                {
                    message = "Event not found."
                });
            }

            return Ok(eventItem);
        }

        // ============================================================
        // GET EVENTS BY CATEGORY
        // GET: api/events/category/1
        // Participant + Organiser
        // ============================================================

        [HttpGet("category/{categoryId:int}")]
        [Authorize(Roles = "Participant,Organiser")]
        public async Task<IActionResult> GetEventsByCategory(int categoryId)
        {
            var categoryExists = await _context.Categories
                .AnyAsync(c => c.CategoryId == categoryId);

            if (!categoryExists)
            {
                return NotFound(new
                {
                    message = "Category not found."
                });
            }

            var events = await _context.Events
                .Include(e => e.Category)
                .Include(e => e.Route)
                .Where(e => e.CategoryId == categoryId)
                .OrderBy(e => e.Date)
                .ThenBy(e => e.StartTime)
                .AsNoTracking()
                .ToListAsync();

            return Ok(events);
        }

        // ============================================================
        // GET EVENTS BY ROUTE
        // GET: api/events/route/1
        // Participant + Organiser
        // ============================================================

        [HttpGet("route/{routeId:int}")]
        [Authorize(Roles = "Participant,Organiser")]
        public async Task<IActionResult> GetEventsByRoute(int routeId)
        {
            var routeExists = await _context.Routes
                .AnyAsync(r => r.RouteId == routeId);

            if (!routeExists)
            {
                return NotFound(new
                {
                    message = "Route not found."
                });
            }

            var events = await _context.Events
                .Include(e => e.Category)
                .Include(e => e.Route)
                .Where(e => e.RouteId == routeId)
                .OrderBy(e => e.Date)
                .ThenBy(e => e.StartTime)
                .AsNoTracking()
                .ToListAsync();

            return Ok(events);
        }

        // ============================================================
        // GET MY EVENTS
        // GET: api/events/my-events
        // Organiser only
        // ============================================================

        [HttpGet("my-events")]
        [Authorize(Roles = "Organiser")]
        public async Task<IActionResult> GetMyEvents()
        {
            var userId = GetCurrentUserId();

            if (userId == null)
            {
                return Unauthorized(new
                {
                    message = "User ID could not be determined from the token."
                });
            }

            var events = await _context.Events
                .Include(e => e.Category)
                .Include(e => e.Route)
                .Where(e => e.UserId == userId.Value)
                .OrderBy(e => e.Date)
                .ThenBy(e => e.StartTime)
                .AsNoTracking()
                .ToListAsync();

            return Ok(events);
        }

        // ============================================================
        // CREATE EVENT
        // POST: api/events
        // Organiser only
        // ============================================================

        [HttpPost]
        [Authorize(Roles = "Organiser")]
        public async Task<IActionResult> CreateEvent(
            [FromBody] Event eventModel)
        {
            // --------------------------------------------------------
            // Get organiser ID from JWT
            // --------------------------------------------------------

            var userId = GetCurrentUserId();

            if (userId == null)
            {
                return Unauthorized(new
                {
                    message = "User ID could not be determined from the token."
                });
            }

            // --------------------------------------------------------
            // Check that the organiser exists
            // --------------------------------------------------------

            var organiserExists = await _context.Organisers
                .AnyAsync(o => o.UserId == userId.Value);

            if (!organiserExists)
            {
                return BadRequest(new
                {
                    message = "The authenticated user is not a registered organiser."
                });
            }

            // --------------------------------------------------------
            // Validate title
            // --------------------------------------------------------

            if (string.IsNullOrWhiteSpace(eventModel.Title))
            {
                return BadRequest(new
                {
                    message = "Event title is required."
                });
            }

            // --------------------------------------------------------
            // Validate category
            // --------------------------------------------------------

            var categoryExists = await _context.Categories
                .AnyAsync(c => c.CategoryId == eventModel.CategoryId);

            if (!categoryExists)
            {
                return BadRequest(new
                {
                    message = "The specified category does not exist."
                });
            }

            // --------------------------------------------------------
            // Validate route
            // --------------------------------------------------------

            var routeExists = await _context.Routes
                .AnyAsync(r => r.RouteId == eventModel.RouteId);

            if (!routeExists)
            {
                return BadRequest(new
                {
                    message = "The specified route does not exist."
                });
            }

            // --------------------------------------------------------
            // Validate distance
            // --------------------------------------------------------

            if (eventModel.Distance < 0)
            {
                return BadRequest(new
                {
                    message = "Distance cannot be negative."
                });
            }

            // --------------------------------------------------------
            // Validate date
            // --------------------------------------------------------

            if (eventModel.Date == default)
            {
                return BadRequest(new
                {
                    message = "Event date is required."
                });
            }

            // --------------------------------------------------------
            // Validate start time
            // --------------------------------------------------------

            if (eventModel.StartTime < TimeSpan.Zero ||
                eventModel.StartTime >= TimeSpan.FromDays(1))
            {
                return BadRequest(new
                {
                    message = "StartTime must be a valid time."
                });
            }

            // --------------------------------------------------------
            // IMPORTANT
            //
            // Never trust UserId supplied by the client.
            // Use the UserId from the JWT.
            // --------------------------------------------------------

            eventModel.UserId = userId.Value;

            // --------------------------------------------------------
            // Do not allow client to manually set EventId
            // --------------------------------------------------------

            eventModel.EventId = 0;

            // --------------------------------------------------------
            // Clean strings
            // --------------------------------------------------------

            eventModel.Title = eventModel.Title.Trim();

            eventModel.Description =
                string.IsNullOrWhiteSpace(eventModel.Description)
                    ? null
                    : eventModel.Description.Trim();

            eventModel.StartLocation =
                eventModel.StartLocation?.Trim() ?? string.Empty;

            eventModel.EndLocation =
                eventModel.EndLocation?.Trim() ?? string.Empty;

            eventModel.ImageUrl =
                string.IsNullOrWhiteSpace(eventModel.ImageUrl)
                    ? null
                    : eventModel.ImageUrl.Trim();

            // --------------------------------------------------------
            // Add event
            // --------------------------------------------------------

            _context.Events.Add(eventModel);

            await _context.SaveChangesAsync();

            return CreatedAtAction(
                nameof(GetEvent),
                new
                {
                    id = eventModel.EventId
                },
                eventModel
            );
        }

        // ============================================================
        // UPDATE EVENT
        // PUT: api/events/1
        // Organiser only
        // ============================================================

        [HttpPut("{id:int}")]
        [Authorize(Roles = "Organiser")]
        public async Task<IActionResult> UpdateEvent(
            int id,
            [FromBody] Event eventModel)
        {
            var userId = GetCurrentUserId();

            if (userId == null)
            {
                return Unauthorized(new
                {
                    message = "User ID could not be determined from the token."
                });
            }

            // --------------------------------------------------------
            // Find event
            // --------------------------------------------------------

            var existingEvent = await _context.Events
                .FirstOrDefaultAsync(e => e.EventId == id);

            if (existingEvent == null)
            {
                return NotFound(new
                {
                    message = "Event not found."
                });
            }

            // --------------------------------------------------------
            // Check ownership
            // --------------------------------------------------------

            if (existingEvent.UserId != userId.Value)
            {
                return Forbid();
            }

            // --------------------------------------------------------
            // Validate title
            // --------------------------------------------------------

            if (string.IsNullOrWhiteSpace(eventModel.Title))
            {
                return BadRequest(new
                {
                    message = "Event title is required."
                });
            }

            // --------------------------------------------------------
            // Validate category
            // --------------------------------------------------------

            var categoryExists = await _context.Categories
                .AnyAsync(c => c.CategoryId == eventModel.CategoryId);

            if (!categoryExists)
            {
                return BadRequest(new
                {
                    message = "The specified category does not exist."
                });
            }

            // --------------------------------------------------------
            // Validate route
            // --------------------------------------------------------

            var routeExists = await _context.Routes
                .AnyAsync(r => r.RouteId == eventModel.RouteId);

            if (!routeExists)
            {
                return BadRequest(new
                {
                    message = "The specified route does not exist."
                });
            }

            // --------------------------------------------------------
            // Validate distance
            // --------------------------------------------------------

            if (eventModel.Distance < 0)
            {
                return BadRequest(new
                {
                    message = "Distance cannot be negative."
                });
            }

            // --------------------------------------------------------
            // Validate date
            // --------------------------------------------------------

            if (eventModel.Date == default)
            {
                return BadRequest(new
                {
                    message = "Event date is required."
                });
            }

            // --------------------------------------------------------
            // Validate time
            // --------------------------------------------------------

            if (eventModel.StartTime < TimeSpan.Zero ||
                eventModel.StartTime >= TimeSpan.FromDays(1))
            {
                return BadRequest(new
                {
                    message = "StartTime must be a valid time."
                });
            }

            // --------------------------------------------------------
            // Update properties
            //
            // UserId remains the original organiser.
            // EventId remains the original ID.
            // --------------------------------------------------------

            existingEvent.CategoryId = eventModel.CategoryId;

            existingEvent.RouteId = eventModel.RouteId;

            existingEvent.Title = eventModel.Title.Trim();

            existingEvent.Description =
                string.IsNullOrWhiteSpace(eventModel.Description)
                    ? null
                    : eventModel.Description.Trim();

            existingEvent.StartLocation =
                eventModel.StartLocation?.Trim() ?? string.Empty;

            existingEvent.EndLocation =
                eventModel.EndLocation?.Trim() ?? string.Empty;

            existingEvent.StartTime = eventModel.StartTime;

            existingEvent.Distance = eventModel.Distance;

            existingEvent.Date = eventModel.Date;

            existingEvent.ImageUrl =
                string.IsNullOrWhiteSpace(eventModel.ImageUrl)
                    ? null
                    : eventModel.ImageUrl.Trim();

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Event updated successfully.",
                eventData = existingEvent
            });
        }

        // ============================================================
        // DELETE EVENT
        // DELETE: api/events/1
        // Organiser only
        // ============================================================

        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Organiser")]
        public async Task<IActionResult> DeleteEvent(int id)
        {
            var userId = GetCurrentUserId();

            if (userId == null)
            {
                return Unauthorized(new
                {
                    message = "User ID could not be determined from the token."
                });
            }

            // --------------------------------------------------------
            // Find event
            // --------------------------------------------------------

            var eventItem = await _context.Events
                .Include(e => e.Enrolments)
                .FirstOrDefaultAsync(e => e.EventId == id);

            if (eventItem == null)
            {
                return NotFound(new
                {
                    message = "Event not found."
                });
            }

            // --------------------------------------------------------
            // Check ownership
            // --------------------------------------------------------

            if (eventItem.UserId != userId.Value)
            {
                return Forbid();
            }

            // --------------------------------------------------------
            // Don't delete event with enrolments
            // --------------------------------------------------------

            if (eventItem.Enrolments.Any())
            {
                return Conflict(new
                {
                    message =
                        "This event cannot be deleted because participants are enrolled.",

                    enrolmentCount = eventItem.Enrolments.Count
                });
            }

            // --------------------------------------------------------
            // Delete
            // --------------------------------------------------------

            _context.Events.Remove(eventItem);

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Event deleted successfully."
            });
        }

        // ============================================================
        // GET CURRENT USER ID
        // ============================================================

        private int? GetCurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);

            if (claim == null)
            {
                return null;
            }

            if (!int.TryParse(claim.Value, out int userId))
            {
                return null;
            }

            return userId;
        }
    }
}