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
    public class ResultsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ResultsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ============================================================
        // GET: api/Results
        // Get all results for events owned by the organiser
        // ============================================================

        [HttpGet]
        [Authorize(Roles = "Organiser")]
        public async Task<IActionResult> GetResults()
        {
            var organiserId = GetCurrentUserId();

            if (organiserId == null)
            {
                return Unauthorized(new
                {
                    message = "User ID could not be determined from the token."
                });
            }

            var results = await _context.Results
                .Where(r => r.Enrolment.Event.UserId == organiserId.Value)
                .AsNoTracking()
                .OrderBy(r => r.Enrolment.Event.Date)
                .ThenBy(r => r.Time)
                .Select(r => new
                {
                    r.ResultsId,
                    r.EnrolmentId,
                    r.Time,

                    Enrolment = new
                    {
                        r.Enrolment.EnrolmentId,
                        r.Enrolment.EventId,
                        r.Enrolment.UserId,
                        r.Enrolment.CreatedAt
                    },

                    Event = new
                    {
                        r.Enrolment.Event.EventId,
                        r.Enrolment.Event.Title,
                        r.Enrolment.Event.Date,
                        r.Enrolment.Event.StartTime,
                        r.Enrolment.Event.Distance
                    },

                    Participant = new
                    {
                        r.Enrolment.Participant.UserId,
                        r.Enrolment.Participant.Gender,
                        r.Enrolment.Participant.Age,

                        UserAccount = new
                        {
                            r.Enrolment.Participant.UserAccount.FirstName,
                            r.Enrolment.Participant.UserAccount.LastName,
                            r.Enrolment.Participant.UserAccount.EmailAddress
                        }
                    }
                })
                .ToListAsync();

            return Ok(results);
        }

        // ============================================================
        // GET: api/Results/5
        // Get a specific result
        // ============================================================

        [HttpGet("{id:int}")]
        [Authorize(Roles = "Participant,Organiser")]
        public async Task<IActionResult> GetResult(int id)
        {
            var userId = GetCurrentUserId();

            if (userId == null)
            {
                return Unauthorized(new
                {
                    message = "User ID could not be determined from the token."
                });
            }

            var result = await _context.Results
                .Where(r => r.ResultsId == id)
                .AsNoTracking()
                .Select(r => new
                {
                    r.ResultsId,
                    r.EnrolmentId,
                    r.Time,

                    ParticipantId = r.Enrolment.UserId,
                    EventOwnerId = r.Enrolment.Event.UserId,

                    Event = new
                    {
                        r.Enrolment.Event.EventId,
                        r.Enrolment.Event.Title,
                        r.Enrolment.Event.Date,
                        r.Enrolment.Event.Distance
                    },

                    Participant = new
                    {
                        r.Enrolment.Participant.UserId,
                        r.Enrolment.Participant.Gender,
                        r.Enrolment.Participant.Age,

                        UserAccount = new
                        {
                            r.Enrolment.Participant.UserAccount.FirstName,
                            r.Enrolment.Participant.UserAccount.LastName,
                            r.Enrolment.Participant.UserAccount.EmailAddress
                        }
                    }
                })
                .FirstOrDefaultAsync();

            if (result == null)
            {
                return NotFound(new
                {
                    message = "Result not found."
                });
            }

            // Participant can only view their own result
            if (User.IsInRole("Participant") &&
                result.ParticipantId != userId.Value)
            {
                return Forbid();
            }

            // Organiser can only view results for their own events
            if (User.IsInRole("Organiser") &&
                result.EventOwnerId != userId.Value)
            {
                return Forbid();
            }

            return Ok(result);
        }

        // ============================================================
        // GET: api/Results/my-results
        // Get results for logged-in participant
        // ============================================================

        [HttpGet("my-results")]
        [Authorize(Roles = "Participant")]
        public async Task<IActionResult> GetMyResults()
        {
            var userId = GetCurrentUserId();

            if (userId == null)
            {
                return Unauthorized(new
                {
                    message = "User ID could not be determined from the token."
                });
            }

            var results = await _context.Results
                .Where(r => r.Enrolment.UserId == userId.Value)
                .AsNoTracking()
                .OrderByDescending(r => r.Enrolment.Event.Date)
                .ThenBy(r => r.Time)
                .Select(r => new
                {
                    r.ResultsId,
                    r.EnrolmentId,
                    r.Time,

                    Event = new
                    {
                        r.Enrolment.Event.EventId,
                        r.Enrolment.Event.Title,
                        r.Enrolment.Event.Description,
                        r.Enrolment.Event.StartLocation,
                        r.Enrolment.Event.EndLocation,
                        r.Enrolment.Event.StartTime,
                        r.Enrolment.Event.Distance,
                        r.Enrolment.Event.Date,
                        r.Enrolment.Event.ImageUrl
                    },

                    Enrolment = new
                    {
                        r.Enrolment.EnrolmentId,
                        r.Enrolment.CreatedAt
                    }
                })
                .ToListAsync();

            return Ok(results);
        }

        // ============================================================
        // GET: api/Results/event/5
        // Get all results for an event
        // Organiser only
        // ============================================================

        [HttpGet("event/{eventId:int}")]
        [Authorize(Roles = "Organiser")]
        public async Task<IActionResult> GetEventResults(int eventId)
        {
            var organiserId = GetCurrentUserId();

            if (organiserId == null)
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

            if (eventItem.UserId != organiserId.Value)
            {
                return Forbid();
            }

            var results = await _context.Results
                .Where(r => r.Enrolment.EventId == eventId)
                .AsNoTracking()
                .OrderBy(r => r.Time)
                .Select(r => new
                {
                    r.ResultsId,
                    r.EnrolmentId,
                    r.Time,

                    Participant = new
                    {
                        r.Enrolment.Participant.UserId,
                        r.Enrolment.Participant.Gender,
                        r.Enrolment.Participant.Age,

                        UserAccount = new
                        {
                            r.Enrolment.Participant.UserAccount.FirstName,
                            r.Enrolment.Participant.UserAccount.LastName,
                            r.Enrolment.Participant.UserAccount.EmailAddress
                        }
                    }
                })
                .ToListAsync();

            return Ok(results);
        }

        // ============================================================
        // GET: api/Results/participant/5
        // Get results for a participant
        // ============================================================

        [HttpGet("participant/{participantId:int}")]
        [Authorize(Roles = "Participant,Organiser")]
        public async Task<IActionResult> GetParticipantResults(
            int participantId)
        {
            var userId = GetCurrentUserId();

            if (userId == null)
            {
                return Unauthorized(new
                {
                    message = "User ID could not be determined from the token."
                });
            }

            // Participants may only view their own results
            if (User.IsInRole("Participant") &&
                participantId != userId.Value)
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

            var results = await _context.Results
                .Where(r => r.Enrolment.UserId == participantId)
                .AsNoTracking()
                .OrderByDescending(r => r.Enrolment.Event.Date)
                .Select(r => new
                {
                    r.ResultsId,
                    r.EnrolmentId,
                    r.Time,

                    Event = new
                    {
                        r.Enrolment.Event.EventId,
                        r.Enrolment.Event.Title,
                        r.Enrolment.Event.Date,
                        r.Enrolment.Event.Distance
                    }
                })
                .ToListAsync();

            return Ok(results);
        }

        // ============================================================
        // POST: api/Results
        // Create a result
        // Organiser only
        // ============================================================

        [HttpPost]
        [Authorize(Roles = "Organiser")]
        public async Task<IActionResult> CreateResult(
            [FromBody] Result resultModel)
        {
            var organiserId = GetCurrentUserId();

            if (organiserId == null)
            {
                return Unauthorized(new
                {
                    message = "User ID could not be determined from the token."
                });
            }

            if (resultModel == null)
            {
                return BadRequest(new
                {
                    message = "Result data is required."
                });
            }

            if (resultModel.EnrolmentId <= 0)
            {
                return BadRequest(new
                {
                    message = "A valid EnrolmentId is required."
                });
            }

            // --------------------------------------------------------
            // Find enrolment
            // --------------------------------------------------------

            var enrolment = await _context.Enrolments
                .Include(e => e.Event)
                .Include(e => e.Result)
                .FirstOrDefaultAsync(
                    e => e.EnrolmentId == resultModel.EnrolmentId);

            if (enrolment == null)
            {
                return NotFound(new
                {
                    message = "The specified enrolment was not found."
                });
            }

            // --------------------------------------------------------
            // Make sure organiser owns the event
            // --------------------------------------------------------

            if (enrolment.Event.UserId != organiserId.Value)
            {
                return Forbid();
            }

            // --------------------------------------------------------
            // Prevent duplicate result
            // --------------------------------------------------------

            if (enrolment.Result != null)
            {
                return Conflict(new
                {
                    message = "A result already exists for this enrolment.",
                    resultId = enrolment.Result.ResultsId
                });
            }

            // --------------------------------------------------------
            // Validate race time
            // --------------------------------------------------------

            if (resultModel.Time <= TimeSpan.Zero)
            {
                return BadRequest(new
                {
                    message = "Result time must be greater than zero."
                });
            }

            if (resultModel.Time >= TimeSpan.FromDays(1))
            {
                return BadRequest(new
                {
                    message = "Result time must be less than 24 hours."
                });
            }

            // --------------------------------------------------------
            // Create result
            // --------------------------------------------------------

            var result = new Result
            {
                EnrolmentId = enrolment.EnrolmentId,
                Time = resultModel.Time
            };

            _context.Results.Add(result);

            await _context.SaveChangesAsync();

            return CreatedAtAction(
                nameof(GetResult),
                new { id = result.ResultsId },
                new
                {
                    message = "Result created successfully.",
                    result = new
                    {
                        result.ResultsId,
                        result.EnrolmentId,
                        result.Time
                    }
                });
        }

        // ============================================================
        // PUT: api/Results/5
        // Update a result
        // Organiser only
        // ============================================================

        [HttpPut("{id:int}")]
        [Authorize(Roles = "Organiser")]
        public async Task<IActionResult> UpdateResult(
            int id,
            [FromBody] Result resultModel)
        {
            var organiserId = GetCurrentUserId();

            if (organiserId == null)
            {
                return Unauthorized(new
                {
                    message = "User ID could not be determined from the token."
                });
            }

            if (resultModel == null)
            {
                return BadRequest(new
                {
                    message = "Result data is required."
                });
            }

            var result = await _context.Results
                .Include(r => r.Enrolment)
                    .ThenInclude(e => e.Event)
                .FirstOrDefaultAsync(r => r.ResultsId == id);

            if (result == null)
            {
                return NotFound(new
                {
                    message = "Result not found."
                });
            }

            // --------------------------------------------------------
            // Check event ownership
            // --------------------------------------------------------

            if (result.Enrolment.Event.UserId != organiserId.Value)
            {
                return Forbid();
            }

            // --------------------------------------------------------
            // Validate time
            // --------------------------------------------------------

            if (resultModel.Time <= TimeSpan.Zero)
            {
                return BadRequest(new
                {
                    message = "Result time must be greater than zero."
                });
            }

            if (resultModel.Time >= TimeSpan.FromDays(1))
            {
                return BadRequest(new
                {
                    message = "Result time must be less than 24 hours."
                });
            }

            // --------------------------------------------------------
            // Update only the time
            // --------------------------------------------------------

            result.Time = resultModel.Time;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Result updated successfully.",
                result = new
                {
                    result.ResultsId,
                    result.EnrolmentId,
                    result.Time
                }
            });
        }

        // ============================================================
        // DELETE: api/Results/5
        // Delete a result
        // Organiser only
        // ============================================================

        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Organiser")]
        public async Task<IActionResult> DeleteResult(int id)
        {
            var organiserId = GetCurrentUserId();

            if (organiserId == null)
            {
                return Unauthorized(new
                {
                    message = "User ID could not be determined from the token."
                });
            }

            var result = await _context.Results
                .Include(r => r.Enrolment)
                    .ThenInclude(e => e.Event)
                .FirstOrDefaultAsync(r => r.ResultsId == id);

            if (result == null)
            {
                return NotFound(new
                {
                    message = "Result not found."
                });
            }

            // --------------------------------------------------------
            // Check event ownership
            // --------------------------------------------------------

            if (result.Enrolment.Event.UserId != organiserId.Value)
            {
                return Forbid();
            }

            _context.Results.Remove(result);

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Result deleted successfully."
            });
        }

        // ============================================================
        // Helper method
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