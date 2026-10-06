namespace race_app_api_test
{
    using FakeItEasy;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Configuration;
    using race_day_backend_api.Controllers;
    using race_day_backend_api.Data;
    using race_day_backend_api.Models;
    using race_day_backend_api.Models.Auth;

    public class UnitTest1
    {
        // =========================================================
        // Create an in-memory database
        // =========================================================

        private ApplicationDbContext CreateDbContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            return new ApplicationDbContext(options);
        }


        // =========================================================
        // Fake JWT configuration
        // =========================================================

        private IConfiguration CreateConfiguration()
        {
            var configuration = A.Fake<IConfiguration>();

            A.CallTo(() => configuration["Jwt:Key"])
                .Returns("ThisIsATestJwtKey12345678901234567890");

            A.CallTo(() => configuration["Jwt:Issuer"])
                .Returns("RaceDayTest");

            A.CallTo(() => configuration["Jwt:Audience"])
                .Returns("RaceDayTestUsers");

            return configuration;
        }


        // =========================================================
        // TEST 1
        // Valid participant registration
        // =========================================================

        [Fact]
        public async Task Register_ValidParticipant_ReturnsOk()
        {
            // Arrange
            using var context = CreateDbContext();

            var configuration = CreateConfiguration();

            var authController = new AuthController(
                context,
                configuration
            );

            var request = new RegisterRequest
            {
                FirstName = "John",
                LastName = "Doe",
                EmailAddress = "john@example.com",
                Password = "Password123!",
                Role = "Participant",
                Gender = "Male",
                Age = 25
            };

            // Act
            var result = await authController.Register(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);

            Assert.NotNull(okResult.Value);

            // Check that the user was actually added
            var user = await context.UserAccounts
                .FirstOrDefaultAsync(
                    u => u.EmailAddress == "john@example.com"
                );

            Assert.NotNull(user);

            Assert.Equal("John", user.FirstName);
            Assert.Equal("Doe", user.LastName);
            Assert.Equal("Participant", user.Role);

            // Check participant record
            var participant = await context.Participants
                .FirstOrDefaultAsync(
                    p => p.UserId == user.UserId
                );

            Assert.NotNull(participant);

            Assert.Equal(25, participant.Age);
            Assert.Equal("Male", participant.Gender);
        }


        // =========================================================
        // TEST 2
        // Duplicate email
        // =========================================================

        [Fact]
        public async Task Register_ExistingEmail_ReturnsConflict()
        {
            // Arrange
            using var context = CreateDbContext();

            var configuration = CreateConfiguration();

            var authController = new AuthController(
                context,
                configuration
            );

            var firstRequest = new RegisterRequest
            {
                FirstName = "John",
                LastName = "Doe",
                EmailAddress = "john@example.com",
                Password = "Password123!",
                Role = "Participant"
            };

            // Register first user
            await authController.Register(firstRequest);

            var secondRequest = new RegisterRequest
            {
                FirstName = "Jane",
                LastName = "Doe",
                EmailAddress = "john@example.com",
                Password = "Password456!",
                Role = "Participant"
            };

            // Act
            var result = await authController.Register(secondRequest);

            // Assert
            var conflictResult =
                Assert.IsType<ConflictObjectResult>(result);

            Assert.NotNull(conflictResult.Value);

            // There should still only be one user
            var users = await context.UserAccounts
                .Where(u =>
                    u.EmailAddress == "john@example.com")
                .ToListAsync();

            Assert.Single(users);
        }


        // =========================================================
        // TEST 3
        // Invalid role
        // =========================================================

        [Fact]
        public async Task Register_InvalidRole_ReturnsBadRequest()
        {
            // Arrange
            using var context = CreateDbContext();

            var configuration = CreateConfiguration();

            var authController = new AuthController(
                context,
                configuration
            );

            var request = new RegisterRequest
            {
                FirstName = "John",
                LastName = "Doe",
                EmailAddress = "john@example.com",
                Password = "Password123!",
                Role = "Admin"
            };

            // Act
            var result = await authController.Register(request);

            // Assert
            var badRequest =
                Assert.IsType<BadRequestObjectResult>(result);

            Assert.NotNull(badRequest.Value);

            // User should not have been created
            var users = await context.UserAccounts.ToListAsync();

            Assert.Empty(users);
        }


        // =========================================================
        // TEST 4
        // Missing required fields
        // =========================================================

        [Fact]
        public async Task Register_MissingFields_ReturnsBadRequest()
        {
            // Arrange
            using var context = CreateDbContext();

            var configuration = CreateConfiguration();

            var authController = new AuthController(
                context,
                configuration
            );

            var request = new RegisterRequest
            {
                FirstName = "",
                LastName = "Doe",
                EmailAddress = "john@example.com",
                Password = "Password123!",
                Role = "Participant"
            };

            // Act
            var result = await authController.Register(request);

            // Assert
            var badRequest =
                Assert.IsType<BadRequestObjectResult>(result);

            Assert.NotNull(badRequest.Value);

            var users = await context.UserAccounts.ToListAsync();

            Assert.Empty(users);
        }


        // =========================================================
        // TEST 5
        // Valid login
        // =========================================================

        [Fact]
        public async Task Login_ValidCredentials_ReturnsOk()
        {
            // Arrange
            using var context = CreateDbContext();

            var configuration = CreateConfiguration();

            var authController = new AuthController(
                context,
                configuration
            );

            // Create account first
            var registerRequest = new RegisterRequest
            {
                FirstName = "Jane",
                LastName = "Smith",
                EmailAddress = "jane@example.com",
                Password = "Password123!",
                Role = "Participant"
            };

            await authController.Register(registerRequest);

            var loginRequest = new LoginRequest
            {
                EmailAddress = "jane@example.com",
                Password = "Password123!"
            };

            // Act
            var result = await authController.Login(loginRequest);

            // Assert
            var okResult =
                Assert.IsType<OkObjectResult>(result);

            Assert.NotNull(okResult.Value);

            // Check that token exists
            var tokenProperty =
                okResult.Value
                    .GetType()
                    .GetProperty("token");

            Assert.NotNull(tokenProperty);

            var token =
                tokenProperty.GetValue(okResult.Value) as string;

            Assert.False(string.IsNullOrWhiteSpace(token));
        }


        // =========================================================
        // TEST 6
        // Invalid password
        // =========================================================

        [Fact]
        public async Task Login_InvalidPassword_ReturnsUnauthorized()
        {
            // Arrange
            using var context = CreateDbContext();

            var configuration = CreateConfiguration();

            var authController = new AuthController(
                context,
                configuration
            );

            var registerRequest = new RegisterRequest
            {
                FirstName = "Jane",
                LastName = "Smith",
                EmailAddress = "jane@example.com",
                Password = "CorrectPassword123!",
                Role = "Participant"
            };

            await authController.Register(registerRequest);

            var loginRequest = new LoginRequest
            {
                EmailAddress = "jane@example.com",
                Password = "WrongPassword123!"
            };

            // Act
            var result = await authController.Login(loginRequest);

            // Assert
            var unauthorizedResult =
                Assert.IsType<UnauthorizedObjectResult>(result);

            Assert.NotNull(unauthorizedResult.Value);
        }
    }
}