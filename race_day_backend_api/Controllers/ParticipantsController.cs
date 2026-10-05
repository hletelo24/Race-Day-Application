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
    public class ParticipantsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ParticipantsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ============================================================
        // GET: api/Participants
        // Get all participants
        // ============================================================
        [HttpGet]
        [Authorize(Roles = "Participant,Organiser")]
        public async Task<IActionResult> GetParticipants()
        {
            var participants = await _context.Participants
                .Include(p => p.UserAccount)
                .AsNoTracking()
                .OrderBy(p => p.UserAccount.LastName)
                .ThenBy(p => p.UserAccount.FirstName)
                .Select(p => new
                {
                    p.UserId,
                    p.Gender,
                    p.Age,
                    p.Role,

                    UserAccount = new
                    {
                        p.UserAccount.UserId,
                        p.UserAccount.FirstName,
                        p.UserAccount.LastName,
                        p.UserAccount.EmailAddress,
                        p.UserAccount.ImageUrl,
                        p.UserAccount.Role
                    }
                })
                .ToListAsync();

            return Ok(participants);
        }

        // ============================================================
        // GET: api/Participants/5
        // Get participant by ID
        // ============================================================
        [HttpGet("{id:int}")]
        [Authorize(Roles = "Participant,Organiser")]
        public async Task<IActionResult> GetParticipant(int id)
        {
            var participant = await _context.Participants
                .Include(p => p.UserAccount)
                .AsNoTracking()
                .Where(p => p.UserId == id)
                .Select(p => new
                {
                    p.UserId,
                    p.Gender,
                    p.Age,
                    p.Role,

                    UserAccount = new
                    {
                        p.UserAccount.UserId,
                        p.UserAccount.FirstName,
                        p.UserAccount.LastName,
                        p.UserAccount.EmailAddress,
                        p.UserAccount.ImageUrl,
                        p.UserAccount.Role
                    }
                })
                .FirstOrDefaultAsync();

            if (participant == null)
            {
                return NotFound(new
                {
                    message = "Participant not found."
                });
            }

            return Ok(participant);
        }

        // ============================================================
        // GET: api/Participants/me
        // Get currently logged-in participant
        // ============================================================
        [HttpGet("me")]
        [Authorize(Roles = "Participant")]
        public async Task<IActionResult> GetMyProfile()
        {
            var userId = GetCurrentUserId();

            if (userId == null)
            {
                return Unauthorized(new
                {
                    message = "User ID could not be determined from the token."
                });
            }

            var participant = await _context.Participants
                .Include(p => p.UserAccount)
                .AsNoTracking()
                .Where(p => p.UserId == userId.Value)
                .Select(p => new
                {
                    p.UserId,
                    p.Gender,
                    p.Age,
                    p.Role,

                    UserAccount = new
                    {
                        p.UserAccount.UserId,
                        p.UserAccount.FirstName,
                        p.UserAccount.LastName,
                        p.UserAccount.EmailAddress,
                        p.UserAccount.ImageUrl,
                        p.UserAccount.Role
                    }
                })
                .FirstOrDefaultAsync();

            if (participant == null)
            {
                return NotFound(new
                {
                    message = "Participant profile not found."
                });
            }

            return Ok(participant);
        }

        // ============================================================
        // GET: api/Participants/5/enrolments
        // Get enrolments belonging to a participant
        // ============================================================
        [HttpGet("{id:int}/enrolments")]
        [Authorize(Roles = "Participant,Organiser")]
        public async Task<IActionResult> GetParticipantEnrolments(int id)
        {
            var participantExists = await _context.Participants
                .AnyAsync(p => p.UserId == id);

            if (!participantExists)
            {
                return NotFound(new
                {
                    message = "Participant not found."
                });
            }

            var enrolments = await _context.Enrolments
                .Where(e => e.UserId == id)
                .Include(e => e.Event)
                    .ThenInclude(e => e.Category)
                .Include(e => e.Event)
                    .ThenInclude(e => e.Route)
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
                                e.Event.Category.Title
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
        // PUT: api/Participants/5
        // Update participant profile
        // ============================================================
        [HttpPut("{id:int}")]
        [Authorize(Roles = "Participant")]
        public async Task<IActionResult> UpdateParticipant(
            int id,
            [FromBody] Participant participantModel)
        {
            var currentUserId = GetCurrentUserId();

            if (currentUserId == null)
            {
                return Unauthorized(new
                {
                    message = "User ID could not be determined from the token."
                });
            }

            // Participants may only edit their own profile
            if (currentUserId.Value != id)
            {
                return Forbid();
            }

            var participant = await _context.Participants
                .FirstOrDefaultAsync(p => p.UserId == id);

            if (participant == null)
            {
                return NotFound(new
                {
                    message = "Participant not found."
                });
            }

            // --------------------------------------------------------
            // Validation
            // --------------------------------------------------------

            if (string.IsNullOrWhiteSpace(participantModel.Gender))
            {
                return BadRequest(new
                {
                    message = "Gender is required."
                });
            }

            if (participantModel.Gender.Length > 20)
            {
                return BadRequest(new
                {
                    message = "Gender cannot exceed 20 characters."
                });
            }

            if (participantModel.Age < 1 || participantModel.Age > 120)
            {
                return BadRequest(new
                {
                    message = "Age must be between 1 and 120."
                });
            }

            // --------------------------------------------------------
            // Update only fields that belong to Participant
            // --------------------------------------------------------

            participant.Gender = participantModel.Gender.Trim();
            participant.Age = participantModel.Age;

            // Do not allow the client to change UserId or Role.
            participant.UserId = id;
            participant.Role = "Participant";

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Participant profile updated successfully.",
                participant = new
                {
                    participant.UserId,
                    participant.Gender,
                    participant.Age,
                    participant.Role
                }
            });
        }

        // ============================================================
        // DELETE: api/Participants/5
        // Delete participant profile
        // ============================================================
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Participant")]
        public async Task<IActionResult> DeleteParticipant(int id)
        {
            var currentUserId = GetCurrentUserId();

            if (currentUserId == null)
            {
                return Unauthorized(new
                {
                    message = "User ID could not be determined from the token."
                });
            }

            // Participants may only delete their own profile
            if (currentUserId.Value != id)
            {
                return Forbid();
            }

            var participant = await _context.Participants
                .Include(p => p.Enrolments)
                .FirstOrDefaultAsync(p => p.UserId == id);

            if (participant == null)
            {
                return NotFound(new
                {
                    message = "Participant not found."
                });
            }

            // --------------------------------------------------------
            // Prevent deletion when participant has enrolments
            // --------------------------------------------------------

            if (participant.Enrolments.Any())
            {
                return Conflict(new
                {
                    message =
                        "The participant cannot be deleted because they have existing event enrolments.",
                    enrolmentCount = participant.Enrolments.Count
                });
            }

            _context.Participants.Remove(participant);

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Participant profile deleted successfully."
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