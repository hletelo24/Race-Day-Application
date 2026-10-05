using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using race_day_backend_api.Data;
using race_day_backend_api.Models;
using race_day_backend_api.Models.Auth;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace race_day_backend_api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly PasswordHasher<UserAccount> _passwordHasher;

        public AuthController(
            ApplicationDbContext context,
            IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;

            _passwordHasher = new PasswordHasher<UserAccount>();
        }


        // =========================================================
        // REGISTER
        // POST: api/auth/register
        // =========================================================

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterRequest request)
        {
            // Basic validation
            if (string.IsNullOrWhiteSpace(request.FirstName) ||
                string.IsNullOrWhiteSpace(request.LastName) ||
                string.IsNullOrWhiteSpace(request.EmailAddress) ||
                string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(new
                {
                    message = "First name, last name, email and password are required."
                });
            }

            // Check whether email already exists
            var existingUser = await _context.UserAccounts
                .FirstOrDefaultAsync(u =>
                    u.EmailAddress.ToLower() ==
                    request.EmailAddress.ToLower());

            if (existingUser != null)
            {
                return Conflict(new
                {
                    message = "An account with this email address already exists."
                });
            }

            // Validate role
            var role = request.Role.Trim();

            if (role != "Participant" && role != "Organiser")
            {
                return BadRequest(new
                {
                    message = "Role must be Participant or Organiser."
                });
            }

            // Create user
            var user = new UserAccount
            {
                FirstName = request.FirstName.Trim(),
                LastName = request.LastName.Trim(),
                EmailAddress = request.EmailAddress.Trim().ToLower(),
                Role = role,
                ImageUrl = request.ImageUrl
            };

            // Hash password
            user.PasswordHash = _passwordHasher.HashPassword(
                user,
                request.Password
            );

            // Add user to database
            _context.UserAccounts.Add(user);

            // Save first so UserId is generated
            await _context.SaveChangesAsync();


            // =====================================================
            // PARTICIPANT
            // =====================================================

            if (role == "Participant")
            {
                var participant = new Participant
                {
                    UserId = user.UserId,
                    Gender = request.Gender ?? string.Empty,
                    Age = request.Age ?? 0,
                    Role = "Participant"
                };

                _context.Participants.Add(participant);
            }


            // =====================================================
            // ORGANISER
            // =====================================================

            if (role == "Organiser")
            {
                var organiser = new Organiser
                {
                    UserId = user.UserId,
                    AccessLevel = request.AccessLevel ?? "Standard"
                };

                _context.Organisers.Add(organiser);
            }

            await _context.SaveChangesAsync();


            // Generate JWT
            var token = GenerateJwtToken(user);


            return Ok(new
            {
                message = "Registration successful.",

                token,

                user = new
                {
                    user.UserId,
                    user.FirstName,
                    user.LastName,
                    user.EmailAddress,
                    user.Role,
                    user.ImageUrl
                }
            });
        }


        // =========================================================
        // LOGIN
        // POST: api/auth/login
        // =========================================================

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.EmailAddress) ||
                string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(new
                {
                    message = "Email address and password are required."
                });
            }


            // Find user in SQL Server
            var user = await _context.UserAccounts
                .FirstOrDefaultAsync(u =>
                    u.EmailAddress.ToLower() ==
                    request.EmailAddress.ToLower());


            if (user == null)
            {
                return Unauthorized(new
                {
                    message = "Invalid email address or password."
                });
            }


            // Verify password
            var passwordResult = _passwordHasher.VerifyHashedPassword(
                user,
                user.PasswordHash,
                request.Password
            );


            if (passwordResult == PasswordVerificationResult.Failed)
            {
                return Unauthorized(new
                {
                    message = "Invalid email address or password."
                });
            }


            // Generate JWT
            var token = GenerateJwtToken(user);


            return Ok(new
            {
                message = "Login successful.",

                token,

                user = new
                {
                    user.UserId,
                    user.FirstName,
                    user.LastName,
                    user.EmailAddress,
                    user.Role,
                    user.ImageUrl
                }
            });
        }


        // =========================================================
        // GENERATE JWT
        // =========================================================

        private string GenerateJwtToken(UserAccount user)
        {
            var jwtKey = _configuration["Jwt:Key"];

            if (string.IsNullOrWhiteSpace(jwtKey))
            {
                throw new InvalidOperationException(
                    "JWT key is missing from configuration."
                );
            }


            var claims = new List<Claim>
            {
                new Claim(
                    ClaimTypes.NameIdentifier,
                    user.UserId.ToString()
                ),

                new Claim(
                    ClaimTypes.Name,
                    user.EmailAddress
                ),

                new Claim(
                    ClaimTypes.Email,
                    user.EmailAddress
                ),

                new Claim(
                    ClaimTypes.GivenName,
                    user.FirstName
                ),

                new Claim(
                    ClaimTypes.Surname,
                    user.LastName
                ),

                new Claim(
                    ClaimTypes.Role,
                    user.Role
                )
            };


            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtKey)
            );


            var credentials = new SigningCredentials(
                key,
                SecurityAlgorithms.HmacSha256
            );


            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(4),
                signingCredentials: credentials
            );


            return new JwtSecurityTokenHandler()
                .WriteToken(token);
        }
    }
}
