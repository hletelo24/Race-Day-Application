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
    public class EnrolmentsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public EnrolmentsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ============================================================
        // GET: api/Enrolments
        // Get all enrolments
        // Organisers only
        // ============================================================

        [HttpGet]
        [Authorize(Roles = "Organiser")]
        public async Task<IActionResult> GetEnrolments()
        {
            var enrolments = await _context.Enrolments
                .Include(e => e.Event)
                    .ThenInclude(e => e.Category)
                .Include(e => e.Event)
                    .ThenInclude(e => e.Route)
                .Include(e => e.Participant)
                    .ThenInclude(p => p.UserAccount)
                .Include(e => e.Result)
                .AsNoTracking()
                .OrderByDescending(e => e.CreatedAt)
                .Select(e => new
                {
                    e.EnrolmentId,
                    e.EventId,
                    e.UserId,
                    e.CreatedAt,

                    Event = new
                    {
                        e.Event.EventId,
                        e.Event.Title,
                        e.Event.Description,
                        e.Event.StartLocation,
                        e.Event.EndLocation,
                        e.Event.StartTime,
                        e.Event.Distance,
                        e.Event.Date,
                        e.Event.ImageUrl,

                        Category = e.Event.Category == null
                            ? null
                            : new
                            {
                                e.Event.Category.CategoryId,
                                e.Event.Category.Title,
                                e.Event.Category.Description
                            },

                        Route = e.Event.Route == null
                            ? null
                            : new
                            {
                                e.Event.Route.RouteId,
                                e.Event.Route.RouteName,
                                e.Event.Route.Location,
                                e.Event.Route.Distance,
                                e.Event.Route.ImageUrl
                            }
                    },

                    Participant = new
                    {
                        e.Participant.UserId,
                        e.Participant.Gender,
                        e.Participant.Age,
                        e.Participant.Role,

                        UserAccount = new
                        {
                            e.Participant.UserAccount.UserId,
                            e.Participant.UserAccount.FirstName,
                            e.Participant.UserAccount.LastName,
                            e.Participant.UserAccount.EmailAddress,
                            e.Participant.UserAccount.ImageUrl
                        }
                    },

                    Result = e.Result == null
                        ? null
                        : new
                        {
                            e.Result.ResultsId,
                            e.Result.EnrolmentId,
                            e.Result.Time
                        }
                })
                .ToListAsync();

            return Ok(enrolments);
        }

        // ============================================================
        // GET: api/Enrolments/5
        // Get a specific enrolment
        // Participant can view own
        // Organiser can view enrolments for own event
        // ============================================================

        [HttpGet("{id:int}")]
        [Authorize(Roles = "Participant,Organiser")]
        public async Task<IActionResult> GetEnrolment(int id)
        {
            var enrolment = await _context.Enrolments
                .Include(e => e.Event)
                    .ThenInclude(e => e.Category)
                .Include(e => e.Event)
                    .ThenInclude(e => e.Route)
                .Include(e => e.Participant)
                    .ThenInclude(p => p.UserAccount)
                .Include(e => e.Result)
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.EnrolmentId == id);

            if (enrolment == null)
            {
                return NotFound(new
                {
                    message = "Enrolment not found."
                });
            }

            var currentUserId = GetCurrentUserId();

            if (currentUserId == null)
            {
                return Unauthorized(new
                {
                    message = "User ID could not be determined from the token."
                });
            }

            // Participant can only see their own enrolment
            if (User.IsInRole("Participant"))
            {
                if (enrolment.UserId != currentUserId.Value)
                {
                    return Forbid();
                }
            }

            // Organiser can only see enrolments for their own event
            if (User.IsInRole("Organiser"))
            {
                if (enrolment.Event.UserId != currentUserId.Value)
                {
                    return Forbid();
                }
            }

            return Ok(new
            {
                enrolment.EnrolmentId,
                enrolment.EventId,
                enrolment.UserId,
                enrolment.CreatedAt,

                Event = new
                {
                    enrolment.Event.EventId,
                    enrolment.Event.Title,
                    enrolment.Event.Description,
                    enrolment.Event.StartLocation,
                    enrolment.Event.EndLocation,
                    enrolment.Event.StartTime,
                    enrolment.Event.Distance,
                    enrolment.Event.Date,
                    enrolment.Event.ImageUrl,

                    Category = enrolment.Event.Category == null
                        ? null
                        : new
                        {
                            enrolment.Event.Category.CategoryId,
                            enrolment.Event.Category.Title,
                            enrolment.Event.Category.Description
                        },

                    Route = enrolment.Event.Route == null
                        ? null
                        : new
                        {
                            enrolment.Event.Route.RouteId,
                            enrolment.Event.Route.RouteName,
                            enrolment.Event.Route.Location,
                            enrolment.Event.Route.Distance,
                            enrolment.Event.Route.ImageUrl
                        }
                },

                Participant = new
                {
                    enrolment.Participant.UserId,
                    enrolment.Participant.Gender,
                    enrolment.Participant.Age,
                    enrolment.Participant.Role,

                    UserAccount = new
                    {
                        enrolment.Participant.UserAccount.UserId,
                        enrolment.Participant.UserAccount.FirstName,
                        enrolment.Participant.UserAccount.LastName,
                        enrolment.Participant.UserAccount.EmailAddress,
                        enrolment.Participant.UserAccount.ImageUrl
                    }
                },

                Result = enrolment.Result == null
                    ? null
                    : new
                    {
                        enrolment.Result.ResultsId,
                        enrolment.Result.EnrolmentId,
                        enrolment.Result.Time
                    }
            });
        }

        // ============================================================
        // GET: api/Enrolments/my-enrolments
        // Get enrolments for logged-in participant
        // ============================================================

        [HttpGet("my-enrolments")]
        [Authorize(Roles = "Participant")]
        public async Task<IActionResult> GetMyEnrolments()
        {
            var userId = GetCurrentUserId();

            if (userId == null)
            {
                return Unauthorized(new
                {
                    message = "User ID could not be determined from the token."
                });
            }

            var enrolments = await _context.Enrolments
                .Where(e => e.UserId == userId.Value)
                .Include(e => e.Event)
                    .ThenInclude(e => e.Category)
                .Include(e => e.Event)
                    .ThenInclude(e => e.Route)
                .Include(e => e.Result)
                .AsNoTracking()
                .OrderByDescending(e => e.Event.Date)
                .Select(e => new
                {
                    e.EnrolmentId,
                    e.EventId,
                    e.UserId,
                    e.CreatedAt,

                    Event = new
                    {
                        e.Event.EventId,
                        e.Event.Title,
                        e.Event.Description,
                        e.Event.StartLocation,
                        e.Event.EndLocation,
                        e.Event.StartTime,
                        e.Event.Distance,
                        e.Event.Date,
                        e.Event.ImageUrl,

                        Category = e.Event.Category == null
                            ? null
                            : new
                            {
                                e.Event.Category.CategoryId,
                                e.Event.Category.Title
                            },

                        Route = e.Event.Route == null
                            ? null
                            : new
                            {
                                e.Event.Route.RouteId,
                                e.Event.Route.RouteName,
                                e.Event.Route.Location,
                                e.Event.Route.Distance
                            }
                    },

                    Result = e.Result == null
                        ? null
                        : new
                        {
                            e.Result.ResultsId,
                            e.Result.EnrolmentId,
                            e.Result.Time
                        }
                })
                .ToListAsync();

            return Ok(enrolments);
        }

        // ============================================================
        // GET: api/Enrolments/event/5
        // Get all enrolments for an event
        // Only the organiser of the event can access this
        // ============================================================

        [HttpGet("event/{eventId:int}")]
        [Authorize(Roles = "Organiser")]
        public async Task<IActionResult> GetEventEnrolments(int eventId)
        {
            var userId = GetCurrentUserId();

            if (userId == null)
            {
                return Unauthorized(new
                {
                    message = "User ID could not be determined from the token."
                });
            }

            var eventItem = await _context.Events
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.EventId == eventId);

            if (eventItem == null)
            {
                return NotFound(new
                {
                    message = "Event not found."
                });
            }

            if (eventItem.UserId != userId.Value)
            {
                return Forbid();
            }

            var enrolments = await _context.Enrolments
                .Where(e => e.EventId == eventId)
                .Include(e => e.Participant)
                    .ThenInclude(p => p.UserAccount)
                .Include(e => e.Result)
                .AsNoTracking()
                .OrderBy(e => e.CreatedAt)
                .Select(e => new
                {
                    e.EnrolmentId,
                    e.EventId,
                    e.UserId,
                    e.CreatedAt,

                    Participant = new
                    {
                        e.Participant.UserId,
                        e.Participant.Gender,
                        e.Participant.Age,
                        e.Participant.Role,

                        UserAccount = new
                        {
                            e.Participant.UserAccount.UserId,
                            e.Participant.UserAccount.FirstName,
                            e.Participant.UserAccount.LastName,
                            e.Participant.UserAccount.EmailAddress,
                            e.Participant.UserAccount.ImageUrl
                        }
                    },

                    Result = e.Result == null
                        ? null
                        : new
                        {
                            e.Result.ResultsId,
                            e.Result.EnrolmentId,
                            e.Result.Time
                        }
                })
                .ToListAsync();

            return Ok(enrolments);
        }

        // ============================================================
        // POST: api/Enrolments
        // Register the logged-in participant for an event
        // ============================================================

        [HttpPost]
        [Authorize(Roles = "Participant")]
        public async Task<IActionResult> CreateEnrolment(
            [FromBody] Enrolment enrolmentModel)
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
            // Make sure participant exists
            // --------------------------------------------------------

            var participantExists = await _context.Participants
                .AnyAsync(p => p.UserId == userId.Value);

            if (!participantExists)
            {
                return BadRequest(new
                {
                    message = "The authenticated user does not have a participant profile."
                });
            }

            // --------------------------------------------------------
            // Validate EventId
            // --------------------------------------------------------

            if (enrolmentModel.EventId <= 0)
            {
                return BadRequest(new
                {
                    message = "A valid EventId is required."
                });
            }

            // --------------------------------------------------------
            // Find event
            // --------------------------------------------------------

            var eventItem = await _context.Events
                .FirstOrDefaultAsync(e => e.EventId == enrolmentModel.EventId);

            if (eventItem == null)
            {
                return NotFound(new
                {
                    message = "The specified event was not found."
                });
            }

            // --------------------------------------------------------
            // Prevent registration for past events
            // --------------------------------------------------------

            var eventDateTime = eventItem.Date.Date
                                .Add(eventItem.StartTime);

            if (eventDateTime <= DateTime.Now)
            {
                return BadRequest(new
                {
                    message = "You cannot enrol in an event that has already started."
                });
            }

            // --------------------------------------------------------
            // Prevent duplicate enrolment
            // --------------------------------------------------------

            var alreadyEnrolled = await _context.Enrolments
                .AnyAsync(e =>
                    e.EventId == enrolmentModel.EventId &&
                    e.UserId == userId.Value);

            if (alreadyEnrolled)
            {
                return Conflict(new
                {
                    message = "You are already enrolled in this event."
                });
            }

            // --------------------------------------------------------
            // Create enrolment
            // --------------------------------------------------------

            var enrolment = new Enrolment
            {
                EventId = enrolmentModel.EventId,
                UserId = userId.Value,
                CreatedAt = DateTime.UtcNow
            };

            _context.Enrolments.Add(enrolment);

            await _context.SaveChangesAsync();

            return CreatedAtAction(
                nameof(GetEnrolment),
                new { id = enrolment.EnrolmentId },
                new
                {
                    message = "Successfully enrolled in the event.",
                    enrolment = new
                    {
                        enrolment.EnrolmentId,
                        enrolment.EventId,
                        enrolment.UserId,
                        enrolment.CreatedAt
                    }
                });
        }

        // ============================================================
        // DELETE: api/Enrolments/5
        // Participant cancels their own enrolment
        // Organiser can remove enrolment from their own event
        // ============================================================

        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Participant,Organiser")]
        public async Task<IActionResult> DeleteEnrolment(int id)
        {
            var userId = GetCurrentUserId();

            if (userId == null)
            {
                return Unauthorized(new
                {
                    message = "User ID could not be determined from the token."
                });
            }

            var enrolment = await _context.Enrolments
                .Include(e => e.Event)
                .FirstOrDefaultAsync(e => e.EnrolmentId == id);

            if (enrolment == null)
            {
                return NotFound(new
                {
                    message = "Enrolment not found."
                });
            }

            // --------------------------------------------------------
            // Participant can only cancel own enrolment
            // --------------------------------------------------------

            if (User.IsInRole("Participant"))
            {
                if (enrolment.UserId != userId.Value)
                {
                    return Forbid();
                }
            }

            // --------------------------------------------------------
            // Organiser can only remove enrolments from own event
            // --------------------------------------------------------

            if (User.IsInRole("Organiser"))
            {
                if (enrolment.Event.UserId != userId.Value)
                {
                    return Forbid();
                }
            }

            _context.Enrolments.Remove(enrolment);

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Enrolment deleted successfully."
            });
        }

        // ============================================================
        // GET: api/Enrolments/participant/5
        // Get enrolments for a specific participant
        // ============================================================

        [HttpGet("participant/{participantId:int}")]
        [Authorize(Roles = "Participant,Organiser")]
        public async Task<IActionResult> GetParticipantEnrolments(
            int participantId)
        {
            var currentUserId = GetCurrentUserId();

            if (currentUserId == null)
            {
                return Unauthorized(new
                {
                    message = "User ID could not be determined from the token."
                });
            }

            // Participants may only view their own enrolments
            if (User.IsInRole("Participant") &&
                currentUserId.Value != participantId)
            {
                return Forbid();
            }

            var participantExists = await _context.Participants
                .AnyAsync(p => p.UserId == participantId);

            if (!participantExists)
            {
                return NotFound(new
                {
                    message = "Participant not found."
                });
            }

            var enrolments = await _context.Enrolments
                .Where(e => e.UserId == participantId)
                .Include(e => e.Event)
                .Include(e => e.Result)
                .AsNoTracking()
                .OrderByDescending(e => e.CreatedAt)
                .Select(e => new
                {
                    e.EnrolmentId,
                    e.EventId,
                    e.UserId,
                    e.CreatedAt,

                    Event = new
                    {
                        e.Event.EventId,
                        e.Event.Title,
                        e.Event.Description,
                        e.Event.StartLocation,
                        e.Event.EndLocation,
                        e.Event.StartTime,
                        e.Event.Distance,
                        e.Event.Date,
                        e.Event.ImageUrl
                    },

                    Result = e.Result == null
                        ? null
                        : new
                        {
                            e.Result.ResultsId,
                            e.Result.EnrolmentId,
                            e.Result.Time
                        }
                })
                .ToListAsync();

            return Ok(enrolments);
        }

        // ============================================================
        // GET: api/Enrolments/5/result
        // Get result for an enrolment
        // ============================================================

        [HttpGet("{id:int}/result")]
        [Authorize(Roles = "Participant,Organiser")]
        public async Task<IActionResult> GetEnrolmentResult(int id)
        {
            var userId = GetCurrentUserId();

            if (userId == null)
            {
                return Unauthorized(new
                {
                    message = "User ID could not be determined from the token."
                });
            }

            var enrolment = await _context.Enrolments
                .Include(e => e.Event)
                .Include(e => e.Result)
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.EnrolmentId == id);

            if (enrolment == null)
            {
                return NotFound(new
                {
                    message = "Enrolment not found."
                });
            }

            // Participant can only view own result
            if (User.IsInRole("Participant") &&
                enrolment.UserId != userId.Value)
            {
                return Forbid();
            }

            // Organiser can only view results for own event
            if (User.IsInRole("Organiser") &&
                enrolment.Event.UserId != userId.Value)
            {
                return Forbid();
            }

            if (enrolment.Result == null)
            {
                return NotFound(new
                {
                    message = "No result has been recorded for this enrolment."
                });
            }

            return Ok(new
            {
                enrolment.Result.ResultsId,
                enrolment.Result.EnrolmentId,
                enrolment.Result.Time
            });
        }

        // ============================================================
        // Helper
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