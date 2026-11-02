using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using race_day_backend_api.Controllers;
using race_day_backend_api.Data;
using race_day_backend_api.Models;
using System.Security.Claims;
using Xunit;

namespace race_app_api_test
{
    public class EnrollmentTests
    {
        // =========================================================
        // CREATE IN-MEMORY DATABASE
        // =========================================================

        private ApplicationDbContext CreateDbContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            return new ApplicationDbContext(options);
        }


        // =========================================================
        // CREATE CONTROLLER WITH AUTHENTICATED USER
        // =========================================================

        private EnrolmentsController CreateController(
            ApplicationDbContext context,
            int userId,
            string role)
        {
            var controller = new EnrolmentsController(context);

            var claims = new List<Claim>
            {
                new Claim(
                    ClaimTypes.NameIdentifier,
                    userId.ToString()),

                new Claim(
                    ClaimTypes.Role,
                    role)
            };

            var identity = new ClaimsIdentity(
                claims,
                "TestAuthentication");

            var principal = new ClaimsPrincipal(identity);

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = principal
                }
            };

            return controller;
        }


        // =========================================================
        // TEST 1
        // PARTICIPANT CAN ENROL IN A FUTURE EVENT
        // =========================================================

        [Fact]
        public async Task CreateEnrolment_ValidParticipantAndEvent_ReturnsCreated()
        {
            // Arrange
            using var context = CreateDbContext();

            int participantId = 1;

            // Create participant profile
            context.Participants.Add(new Participant
            {
                UserId = participantId,
                Gender = "Male",
                Age = 25,
                Role = "Participant"
            });

            // Create future event
            context.Events.Add(new Event
            {
                EventId = 1,
                UserId = 10,
                Title = "Cape Town Marathon",
                CategoryId = 1,
                RouteId = 1,
                Date = DateTime.Now.Date.AddDays(30),
                StartTime = new TimeSpan(8, 0, 0),
                Distance = 42
            });

            await context.SaveChangesAsync();

            var controller = CreateController(
                context,
                participantId,
                "Participant");

            var request = new Enrolment
            {
                EventId = 1,

                // This should NOT be trusted by the controller.
                UserId = 999
            };

            // Act
            var result =
                await controller.CreateEnrolment(request);

            // Assert
            var createdResult =
                Assert.IsType<CreatedAtActionResult>(result);

            Assert.NotNull(createdResult.Value);

            // Verify database record
            var enrolment =
                await context.Enrolments
                    .FirstOrDefaultAsync();

            Assert.NotNull(enrolment);

            Assert.Equal(1, enrolment.EventId);

            // IMPORTANT:
            // The controller must use the authenticated
            // participant ID instead of request.UserId.
            Assert.Equal(
                participantId,
                enrolment.UserId);

            Assert.NotEqual(
                999,
                enrolment.UserId);

            // CreatedAt should be populated
            Assert.NotEqual(
                default,
                enrolment.CreatedAt);
        }


        // =========================================================
        // TEST 2
        // PARTICIPANT CANNOT ENROL WITHOUT PROFILE
        // =========================================================

        [Fact]
        public async Task CreateEnrolment_NoParticipantProfile_ReturnsBadRequest()
        {
            // Arrange
            using var context = CreateDbContext();

            int participantId = 1;

            // No Participant record is created.

            context.Events.Add(new Event
            {
                EventId = 1,
                UserId = 10,
                Title = "Test Marathon",
                CategoryId = 1,
                RouteId = 1,
                Date = DateTime.Now.Date.AddDays(30),
                StartTime = new TimeSpan(8, 0, 0),
                Distance = 10
            });

            await context.SaveChangesAsync();

            var controller = CreateController(
                context,
                participantId,
                "Participant");

            var request = new Enrolment
            {
                EventId = 1
            };

            // Act
            var result =
                await controller.CreateEnrolment(request);

            // Assert
            var badRequest =
                Assert.IsType<BadRequestObjectResult>(result);

            Assert.NotNull(badRequest.Value);

            Assert.Empty(
                await context.Enrolments.ToListAsync());
        }


        // =========================================================
        // TEST 3
        // INVALID EVENT ID
        // =========================================================

        [Fact]
        public async Task CreateEnrolment_InvalidEventId_ReturnsBadRequest()
        {
            // Arrange
            using var context = CreateDbContext();

            int participantId = 1;

            context.Participants.Add(new Participant
            {
                UserId = participantId,
                Gender = "Female",
                Age = 24,
                Role = "Participant"
            });

            await context.SaveChangesAsync();

            var controller = CreateController(
                context,
                participantId,
                "Participant");

            var request = new Enrolment
            {
                EventId = 0
            };

            // Act
            var result =
                await controller.CreateEnrolment(request);

            // Assert
            var badRequest =
                Assert.IsType<BadRequestObjectResult>(result);

            Assert.NotNull(badRequest.Value);

            Assert.Empty(
                await context.Enrolments.ToListAsync());
        }


        // =========================================================
        // TEST 4
        // EVENT DOES NOT EXIST
        // =========================================================

        [Fact]
        public async Task CreateEnrolment_EventDoesNotExist_ReturnsNotFound()
        {
            // Arrange
            using var context = CreateDbContext();

            int participantId = 1;

            context.Participants.Add(new Participant
            {
                UserId = participantId,
                Gender = "Male",
                Age = 30,
                Role = "Participant"
            });

            await context.SaveChangesAsync();

            var controller = CreateController(
                context,
                participantId,
                "Participant");

            var request = new Enrolment
            {
                EventId = 999
            };

            // Act
            var result =
                await controller.CreateEnrolment(request);

            // Assert
            var notFound =
                Assert.IsType<NotFoundObjectResult>(result);

            Assert.NotNull(notFound.Value);

            Assert.Empty(
                await context.Enrolments.ToListAsync());
        }


        // =========================================================
        // TEST 5
        // CANNOT ENROL IN PAST EVENT
        // =========================================================

        [Fact]
        public async Task CreateEnrolment_PastEvent_ReturnsBadRequest()
        {
            // Arrange
            using var context = CreateDbContext();

            int participantId = 1;

            context.Participants.Add(new Participant
            {
                UserId = participantId,
                Gender = "Female",
                Age = 28,
                Role = "Participant"
            });

            // Event occurred yesterday
            context.Events.Add(new Event
            {
                EventId = 1,
                UserId = 10,
                Title = "Past Race",
                CategoryId = 1,
                RouteId = 1,
                Date = DateTime.Now.Date.AddDays(-1),
                StartTime = new TimeSpan(8, 0, 0),
                Distance = 10
            });

            await context.SaveChangesAsync();

            var controller = CreateController(
                context,
                participantId,
                "Participant");

            var request = new Enrolment
            {
                EventId = 1
            };

            // Act
            var result =
                await controller.CreateEnrolment(request);

            // Assert
            var badRequest =
                Assert.IsType<BadRequestObjectResult>(result);

            Assert.NotNull(badRequest.Value);

            Assert.Empty(
                await context.Enrolments.ToListAsync());
        }


        // =========================================================
        // TEST 6
        // DUPLICATE ENROLMENT IS PREVENTED
        // =========================================================

        [Fact]
        public async Task CreateEnrolment_AlreadyEnrolled_ReturnsConflict()
        {
            // Arrange
            using var context = CreateDbContext();

            int participantId = 1;

            context.Participants.Add(new Participant
            {
                UserId = participantId,
                Gender = "Male",
                Age = 25,
                Role = "Participant"
            });

            context.Events.Add(new Event
            {
                EventId = 1,
                UserId = 10,
                Title = "Already Registered Race",
                CategoryId = 1,
                RouteId = 1,
                Date = DateTime.Now.Date.AddDays(30),
                StartTime = new TimeSpan(8, 0, 0),
                Distance = 10
            });

            // Existing enrolment
            context.Enrolments.Add(new Enrolment
            {
                EnrolmentId = 1,
                EventId = 1,
                UserId = participantId,
                CreatedAt = DateTime.UtcNow
            });

            await context.SaveChangesAsync();

            var controller = CreateController(
                context,
                participantId,
                "Participant");

            var request = new Enrolment
            {
                EventId = 1
            };

            // Act
            var result =
                await controller.CreateEnrolment(request);

            // Assert
            var conflict =
                Assert.IsType<ConflictObjectResult>(result);

            Assert.NotNull(conflict.Value);

            // Ensure duplicate wasn't added
            var enrolments =
                await context.Enrolments
                    .Where(e =>
                        e.EventId == 1 &&
                        e.UserId == participantId)
                    .ToListAsync();

            Assert.Single(enrolments);
        }


        // =========================================================
        // TEST 7
        // GET MY ENROLMENTS
        // =========================================================

        

        // =========================================================
        // TEST 8
        // PARTICIPANT CAN DELETE OWN ENROLMENT
        // =========================================================

        [Fact]
        public async Task DeleteEnrolment_OwnEnrolment_ReturnsOk()
        {
            // Arrange
            using var context = CreateDbContext();

            int participantId = 1;

            context.Events.Add(new Event
            {
                EventId = 1,
                UserId = 10,
                Title = "Race",
                CategoryId = 1,
                RouteId = 1,
                Date = DateTime.Now.AddDays(30),
                StartTime = new TimeSpan(8, 0, 0),
                Distance = 10
            });

            context.Enrolments.Add(new Enrolment
            {
                EnrolmentId = 1,
                EventId = 1,
                UserId = participantId,
                CreatedAt = DateTime.UtcNow
            });

            await context.SaveChangesAsync();

            var controller = CreateController(
                context,
                participantId,
                "Participant");

            // Act
            var result =
                await controller.DeleteEnrolment(1);

            // Assert
            var okResult =
                Assert.IsType<OkObjectResult>(result);

            Assert.NotNull(okResult.Value);

            var deleted =
                await context.Enrolments
                    .FirstOrDefaultAsync(
                        e => e.EnrolmentId == 1);

            Assert.Null(deleted);
        }


        // =========================================================
        // TEST 9
        // PARTICIPANT CANNOT DELETE SOMEONE ELSE'S ENROLMENT
        // =========================================================

        [Fact]
        public async Task DeleteEnrolment_NotOwnEnrolment_ReturnsForbid()
        {
            // Arrange
            using var context = CreateDbContext();

            context.Events.Add(new Event
            {
                EventId = 1,
                UserId = 10,
                Title = "Race",
                CategoryId = 1,
                RouteId = 1,
                Date = DateTime.Now.AddDays(30),
                StartTime = new TimeSpan(8, 0, 0),
                Distance = 10
            });

            // Enrolment belongs to participant 99
            context.Enrolments.Add(new Enrolment
            {
                EnrolmentId = 1,
                EventId = 1,
                UserId = 99,
                CreatedAt = DateTime.UtcNow
            });

            await context.SaveChangesAsync();

            // Participant 1 tries to delete it
            var controller = CreateController(
                context,
                1,
                "Participant");

            // Act
            var result =
                await controller.DeleteEnrolment(1);

            // Assert
            Assert.IsType<ForbidResult>(result);

            // Enrolment should still exist
            var enrolment =
                await context.Enrolments
                    .FirstOrDefaultAsync(
                        e => e.EnrolmentId == 1);

            Assert.NotNull(enrolment);
        }
    }
}