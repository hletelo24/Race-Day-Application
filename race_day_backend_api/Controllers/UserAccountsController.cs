using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using race_day_backend_api.Data;
using race_day_backend_api.Models;
using System.Security.Claims;

namespace race_day_backend_api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class UserAccountsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly PasswordHasher<UserAccount> _passwordHasher;

        public UserAccountsController(
            ApplicationDbContext context,
            PasswordHasher<UserAccount> passwordHasher)
        {
            _context = context;
            _passwordHasher = passwordHasher;
        }

        // ============================================================
        // GET: api/useraccounts
        // Organiser only
        // ============================================================

        [HttpGet]
        [Authorize(Roles = "Organiser")]
        public async Task<IActionResult> GetUserAccounts()
        {
            var users = await _context.UserAccounts
                .AsNoTracking()
                .Select(u => new
                {
                    u.UserId,
                    u.FirstName,
                    u.LastName,
                    u.EmailAddress,
                    u.ImageUrl,
                    u.Role
                })
                .ToListAsync();

            return Ok(users);
        }

        // ============================================================
        // GET: api/useraccounts/{id}
        // Authenticated users can view an account
        // ============================================================

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetUserAccount(int id)
        {
            var user = await _context.UserAccounts
                .AsNoTracking()
                .Where(u => u.UserId == id)
                .Select(u => new
                {
                    u.UserId,
                    u.FirstName,
                    u.LastName,
                    u.EmailAddress,
                    u.ImageUrl,
                    u.Role
                })
                .FirstOrDefaultAsync();

            if (user == null)
            {
                return NotFound(new
                {
                    message = "User account not found."
                });
            }

            return Ok(user);
        }

        // ============================================================
        // GET: api/useraccounts/me
        // Gets currently authenticated user
        // ============================================================

        [HttpGet("me")]
        public async Task<IActionResult> GetMyAccount()
        {
            var userId = GetCurrentUserId();

            if (userId == null)
            {
                return Unauthorized(new
                {
                    message = "Invalid authentication token."
                });
            }

            var user = await _context.UserAccounts
                .AsNoTracking()
                .Where(u => u.UserId == userId.Value)
                .Select(u => new
                {
                    u.UserId,
                    u.FirstName,
                    u.LastName,
                    u.EmailAddress,
                    u.ImageUrl,
                    u.Role
                })
                .FirstOrDefaultAsync();

            if (user == null)
            {
                return NotFound(new
                {
                    message = "User account not found."
                });
            }

            return Ok(user);
        }

        // ============================================================
        // GET: api/useraccounts/{id}/profile
        // Gets account and role-specific profile
        // ============================================================

        [HttpGet("{id:int}/profile")]
        public async Task<IActionResult> GetUserProfile(int id)
        {
            var user = await _context.UserAccounts
                .AsNoTracking()
                .Include(u => u.Participant)
                .Include(u => u.Organiser)
                .FirstOrDefaultAsync(u => u.UserId == id);

            if (user == null)
            {
                return NotFound(new
                {
                    message = "User account not found."
                });
            }

            if (user.Role.Equals("Participant", StringComparison.OrdinalIgnoreCase))
            {
                return Ok(new
                {
                    user.UserId,
                    user.FirstName,
                    user.LastName,
                    user.EmailAddress,
                    user.ImageUrl,
                    user.Role,

                    participant = user.Participant == null
                        ? null
                        : new
                        {
                            user.Participant.Gender,
                            user.Participant.Age,
                            user.Participant.Role
                        }
                });
            }

            if (user.Role.Equals("Organiser", StringComparison.OrdinalIgnoreCase))
            {
                return Ok(new
                {
                    user.UserId,
                    user.FirstName,
                    user.LastName,
                    user.EmailAddress,
                    user.ImageUrl,
                    user.Role,

                    organiser = user.Organiser == null
                        ? null
                        : new
                        {
                            user.Organiser.AccessLevel
                        }
                });
            }

            return Ok(new
            {
                user.UserId,
                user.FirstName,
                user.LastName,
                user.EmailAddress,
                user.ImageUrl,
                user.Role
            });
        }

        // ============================================================
        // PUT: api/useraccounts/{id}
        // User can update their own account
        // ============================================================

        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateUserAccount(
            int id,
            [FromBody] UpdateUserAccountRequest request)
        {
            var currentUserId = GetCurrentUserId();

            if (currentUserId == null)
            {
                return Unauthorized(new
                {
                    message = "Invalid authentication token."
                });
            }

            // Users can only update their own account.
            if (currentUserId.Value != id)
            {
                return Forbid();
            }

            var user = await _context.UserAccounts
                .FirstOrDefaultAsync(u => u.UserId == id);

            if (user == null)
            {
                return NotFound(new
                {
                    message = "User account not found."
                });
            }

            // Validate first name
            if (!string.IsNullOrWhiteSpace(request.FirstName))
            {
                user.FirstName = request.FirstName.Trim();
            }

            // Validate last name
            if (!string.IsNullOrWhiteSpace(request.LastName))
            {
                user.LastName = request.LastName.Trim();
            }

            // Validate email
            if (!string.IsNullOrWhiteSpace(request.EmailAddress))
            {
                var email = request.EmailAddress.Trim().ToLower();

                var emailExists = await _context.UserAccounts
                    .AnyAsync(u =>
                        u.EmailAddress.ToLower() == email &&
                        u.UserId != id);

                if (emailExists)
                {
                    return Conflict(new
                    {
                        message = "An account with this email address already exists."
                    });
                }

                user.EmailAddress = email;
            }

            // Update image URL if supplied
            if (request.ImageUrl != null)
            {
                user.ImageUrl = request.ImageUrl.Trim();
            }

            // Update password if supplied
            if (!string.IsNullOrWhiteSpace(request.Password))
            {
                if (request.Password.Length < 6)
                {
                    return BadRequest(new
                    {
                        message = "Password must contain at least 6 characters."
                    });
                }

                user.PasswordHash = _passwordHasher.HashPassword(
                    user,
                    request.Password);
            }

            // IMPORTANT:
            // Role is deliberately NOT updated here.
            // Users should not be able to promote themselves to Organiser.

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "User account updated successfully.",
                user = new
                {
                    user.UserId,
                    user.FirstName,
                    user.LastName,
                    user.EmailAddress,
                    user.ImageUrl,
                    user.Role
                }
            });
        }

        // ============================================================
        // DELETE: api/useraccounts/{id}
        // User can delete their own account
        // ============================================================

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteUserAccount(int id)
        {
            var currentUserId = GetCurrentUserId();

            if (currentUserId == null)
            {
                return Unauthorized(new
                {
                    message = "Invalid authentication token."
                });
            }

            if (currentUserId.Value != id)
            {
                return Forbid();
            }

            var user = await _context.UserAccounts
                .Include(u => u.Participant)
                    .ThenInclude(p => p!.Enrolments)
                .Include(u => u.Organiser)
                    .ThenInclude(o => o!.Events)
                .FirstOrDefaultAsync(u => u.UserId == id);

            if (user == null)
            {
                return NotFound(new
                {
                    message = "User account not found."
                });
            }

            // --------------------------------------------------------
            // Participant deletion
            // --------------------------------------------------------

            if (user.Participant != null &&
                user.Participant.Enrolments.Any())
            {
                return Conflict(new
                {
                    message = "This account cannot be deleted because the participant has existing enrolments."
                });
            }

            // --------------------------------------------------------
            // Organiser deletion
            // --------------------------------------------------------

            if (user.Organiser != null &&
                user.Organiser.Events.Any())
            {
                return Conflict(new
                {
                    message = "This account cannot be deleted because the organiser has existing events."
                });
            }

            _context.UserAccounts.Remove(user);

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "User account deleted successfully."
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

            if (int.TryParse(claim.Value, out var userId))
            {
                return userId;
            }

            return null;
        }
    }

    // ================================================================
    // Update request
    // ================================================================

    public class UpdateUserAccountRequest
    {
        public string? FirstName { get; set; }

        public string? LastName { get; set; }

        public string? EmailAddress { get; set; }

        public string? Password { get; set; }

        public string? ImageUrl { get; set; }
    }
}