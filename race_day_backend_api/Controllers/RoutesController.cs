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
    public class RoutesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public RoutesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ============================================================
        // GET: api/Routes
        // Get all routes
        // ============================================================

        [HttpGet]
        [Authorize(Roles = "Participant,Organiser")]
        public async Task<IActionResult> GetRoutes()
        {
            var routes = await _context.Routes
                .AsNoTracking()
                .OrderBy(r => r.RouteName)
                .Select(r => new
                {
                    r.RouteId,
                    r.RouteName,
                    r.Location,
                    r.Distance,
                    r.ImageUrl,

                    EventCount = r.Events.Count()
                })
                .ToListAsync();

            return Ok(routes);
        }

        // ============================================================
        // GET: api/Routes/5
        // Get route by ID
        // ============================================================

        [HttpGet("{id:int}")]
        [Authorize(Roles = "Participant,Organiser")]
        public async Task<IActionResult> GetRoute(int id)
        {
            var route = await _context.Routes
                .AsNoTracking()
                .Where(r => r.RouteId == id)
                .Select(r => new
                {
                    r.RouteId,
                    r.RouteName,
                    r.Location,
                    r.Distance,
                    r.ImageUrl,

                    EventCount = r.Events.Count()
                })
                .FirstOrDefaultAsync();

            if (route == null)
            {
                return NotFound(new
                {
                    message = "Route not found."
                });
            }

            return Ok(route);
        }

        // ============================================================
        // GET: api/Routes/5/events
        // Get all events using a route
        // ============================================================

        [HttpGet("{id:int}/events")]
        [Authorize(Roles = "Participant,Organiser")]
        public async Task<IActionResult> GetRouteEvents(int id)
        {
            var routeExists = await _context.Routes
                .AnyAsync(r => r.RouteId == id);

            if (!routeExists)
            {
                return NotFound(new
                {
                    message = "Route not found."
                });
            }

            var events = await _context.Events
                .Where(e => e.RouteId == id)
                .AsNoTracking()
                .OrderBy(e => e.Date)
                .ThenBy(e => e.StartTime)
                .Select(e => new
                {
                    e.EventId,
                    e.UserId,
                    e.CategoryId,
                    e.RouteId,
                    e.Title,
                    e.Description,
                    e.StartLocation,
                    e.EndLocation,
                    e.StartTime,
                    e.Distance,
                    e.Date,
                    e.ImageUrl,

                    Category = e.Category == null
                        ? null
                        : new
                        {
                            e.Category.CategoryId,
                            e.Category.Title,
                            e.Category.Description
                        }
                })
                .ToListAsync();

            return Ok(events);
        }

        // ============================================================
        // GET: api/Routes/search?search=Johannesburg
        // Search routes by name or location
        // ============================================================

        [HttpGet("search")]
        [Authorize(Roles = "Participant,Organiser")]
        public async Task<IActionResult> SearchRoutes(
            [FromQuery] string? search)
        {
            if (string.IsNullOrWhiteSpace(search))
            {
                return BadRequest(new
                {
                    message = "Please provide a search term."
                });
            }

            search = search.Trim();

            var routes = await _context.Routes
                .Where(r =>
                    r.RouteName.Contains(search) ||
                    r.Location.Contains(search))
                .AsNoTracking()
                .OrderBy(r => r.RouteName)
                .Select(r => new
                {
                    r.RouteId,
                    r.RouteName,
                    r.Location,
                    r.Distance,
                    r.ImageUrl,

                    EventCount = r.Events.Count()
                })
                .ToListAsync();

            return Ok(routes);
        }

        // ============================================================
        // POST: api/Routes
        // Create a route
        // Organiser only
        // ============================================================

        [HttpPost]
        [Authorize(Roles = "Organiser")]
        public async Task<IActionResult> CreateRoute(
            [FromBody] Models.Route routeModel)
        {
            if (routeModel == null)
            {
                return BadRequest(new
                {
                    message = "Route data is required."
                });
            }

            // --------------------------------------------------------
            // Validate route name
            // --------------------------------------------------------

            if (string.IsNullOrWhiteSpace(routeModel.RouteName))
            {
                return BadRequest(new
                {
                    message = "Route name is required."
                });
            }

            var routeName = routeModel.RouteName.Trim();

            if (routeName.Length > 150)
            {
                return BadRequest(new
                {
                    message = "Route name cannot exceed 150 characters."
                });
            }

            // --------------------------------------------------------
            // Validate location
            // --------------------------------------------------------

            var location = routeModel.Location?.Trim() ?? string.Empty;

            if (location.Length > 255)
            {
                return BadRequest(new
                {
                    message = "Location cannot exceed 255 characters."
                });
            }

            // --------------------------------------------------------
            // Validate distance
            // --------------------------------------------------------

            if (routeModel.Distance <= 0)
            {
                return BadRequest(new
                {
                    message = "Distance must be greater than zero."
                });
            }

            // --------------------------------------------------------
            // Prevent duplicate route names
            // --------------------------------------------------------

            var duplicateRoute = await _context.Routes
                .AnyAsync(r =>
                    r.RouteName.ToLower() == routeName.ToLower());

            if (duplicateRoute)
            {
                return Conflict(new
                {
                    message = "A route with this name already exists."
                });
            }

            // --------------------------------------------------------
            // Create route
            // --------------------------------------------------------

            var route = new Models.Route
            {
                RouteName = routeName,
                Location = location,
                Distance = routeModel.Distance,
                ImageUrl = string.IsNullOrWhiteSpace(routeModel.ImageUrl)
                    ? null
                    : routeModel.ImageUrl.Trim()
            };

            _context.Routes.Add(route);

            await _context.SaveChangesAsync();

            return CreatedAtAction(
                nameof(GetRoute),
                new { id = route.RouteId },
                new
                {
                    message = "Route created successfully.",
                    route = new
                    {
                        route.RouteId,
                        route.RouteName,
                        route.Location,
                        route.Distance,
                        route.ImageUrl
                    }
                });
        }

        // ============================================================
        // PUT: api/Routes/5
        // Update route
        // Organiser only
        // ============================================================

        [HttpPut("{id:int}")]
        [Authorize(Roles = "Organiser")]
        public async Task<IActionResult> UpdateRoute(
            int id,
            [FromBody] Models.Route routeModel)
        {
            if (routeModel == null)
            {
                return BadRequest(new
                {
                    message = "Route data is required."
                });
            }

            var route = await _context.Routes
                .FirstOrDefaultAsync(r => r.RouteId == id);

            if (route == null)
            {
                return NotFound(new
                {
                    message = "Route not found."
                });
            }

            // --------------------------------------------------------
            // Validate route name
            // --------------------------------------------------------

            if (string.IsNullOrWhiteSpace(routeModel.RouteName))
            {
                return BadRequest(new
                {
                    message = "Route name is required."
                });
            }

            var routeName = routeModel.RouteName.Trim();

            if (routeName.Length > 150)
            {
                return BadRequest(new
                {
                    message = "Route name cannot exceed 150 characters."
                });
            }

            // --------------------------------------------------------
            // Validate location
            // --------------------------------------------------------

            var location = routeModel.Location?.Trim() ?? string.Empty;

            if (location.Length > 255)
            {
                return BadRequest(new
                {
                    message = "Location cannot exceed 255 characters."
                });
            }

            // --------------------------------------------------------
            // Validate distance
            // --------------------------------------------------------

            if (routeModel.Distance <= 0)
            {
                return BadRequest(new
                {
                    message = "Distance must be greater than zero."
                });
            }

            // --------------------------------------------------------
            // Check duplicate name
            // --------------------------------------------------------

            var duplicateRoute = await _context.Routes
                .AnyAsync(r =>
                    r.RouteId != id &&
                    r.RouteName.ToLower() == routeName.ToLower());

            if (duplicateRoute)
            {
                return Conflict(new
                {
                    message = "Another route with this name already exists."
                });
            }

            // --------------------------------------------------------
            // Update
            // --------------------------------------------------------

            route.RouteName = routeName;
            route.Location = location;
            route.Distance = routeModel.Distance;
            route.ImageUrl = string.IsNullOrWhiteSpace(routeModel.ImageUrl)
                ? null
                : routeModel.ImageUrl.Trim();

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Route updated successfully.",
                route = new
                {
                    route.RouteId,
                    route.RouteName,
                    route.Location,
                    route.Distance,
                    route.ImageUrl
                }
            });
        }

        // ============================================================
        // DELETE: api/Routes/5
        // Delete route
        // Organiser only
        // ============================================================

        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Organiser")]
        public async Task<IActionResult> DeleteRoute(int id)
        {
            var route = await _context.Routes
                .Include(r => r.Events)
                .FirstOrDefaultAsync(r => r.RouteId == id);

            if (route == null)
            {
                return NotFound(new
                {
                    message = "Route not found."
                });
            }

            // --------------------------------------------------------
            // Prevent deletion when events use this route
            // --------------------------------------------------------

            if (route.Events.Any())
            {
                return Conflict(new
                {
                    message =
                        "This route cannot be deleted because it is being used by one or more events.",

                    eventCount = route.Events.Count
                });
            }

            _context.Routes.Remove(route);

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Route deleted successfully."
            });
        }
    }
}