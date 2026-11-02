using FakeItEasy;
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
    public class EventsTests
    {
        // =========================================================
        // CREATE DATABASE
        // =========================================================

        private ApplicationDbContext CreateDbContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            return new ApplicationDbContext(options);
        }


        // =========================================================
        // CREATE CONTROLLER WITH USER CLAIM
        // =========================================================

        private EventsController CreateController(
            ApplicationDbContext context,
            int userId)
        {
            var controller = new EventsController(context);

            var claims = new List<Claim>
            {
                new Claim(
                    ClaimTypes.NameIdentifier,
                    userId.ToString())
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
        // GET ALL EVENTS
        // =========================================================

        [Fact]
        public async Task GetEvents_ReturnsAllEvents()
        {
            // Arrange
            using var context = CreateDbContext();

            var category = new Category
            {
                CategoryId = 1
            };

            var route = new Route
            {
                RouteId = 1
            };

            context.Categories.Add(category);
            context.Routes.Add(route);

            context.Events.AddRange(
                new Event
                {
                    EventId = 1,
                    UserId = 1,
                    CategoryId = 1,
                    RouteId = 1,
                    Title = "Cape Town Marathon",
                    Date = new DateTime(2026, 11, 1),
                    StartTime = new TimeSpan(7, 0, 0),
                    Distance = 42
                },

                new Event
                {
                    EventId = 2,
                    UserId = 2,
                    CategoryId = 1,
                    RouteId = 1,
                    Title = "Johannesburg Fun Run",
                    Date = new DateTime(2026, 12, 1),
                    StartTime = new TimeSpan(8, 0, 0),
                    Distance = 10
                }
            );

            await context.SaveChangesAsync();

            var controller = CreateController(context, 1);

            // Act
            var result = await controller.GetEvents();

            // Assert
            var okResult =
                Assert.IsType<OkObjectResult>(result);

            var events =
                Assert.IsAssignableFrom<IEnumerable<Event>>(
                    okResult.Value);

            Assert.Equal(2, events.Count());
        }


        // =========================================================
        // TEST 3
        // GET EVENT THAT DOES NOT EXIST
        // =========================================================

        [Fact]
        public async Task GetEvent_NonExistingEvent_ReturnsNotFound()
        {
            // Arrange
            using var context = CreateDbContext();

            var controller = CreateController(context, 1);

            // Act
            var result = await controller.GetEvent(999);

            // Assert
            var notFoundResult =
                Assert.IsType<NotFoundObjectResult>(result);

            Assert.NotNull(notFoundResult.Value);
        }


        // =========================================================
        // TEST 4
        // CREATE EVENT SUCCESSFULLY
        // =========================================================

        [Fact]
        public async Task CreateEvent_ValidEvent_ReturnsCreated()
        {
            // Arrange
            using var context = CreateDbContext();

            int organiserId = 1;

            // Organiser must exist
            context.Organisers.Add(new Organiser
            {
                UserId = organiserId
            });

            // Category must exist
            context.Categories.Add(new Category
            {
                CategoryId = 1
            });

            // Route must exist
            context.Routes.Add(new Route
            {
                RouteId = 1
            });

            await context.SaveChangesAsync();

            var controller =
                CreateController(context, organiserId);

            var eventModel = new Event
            {
                EventId = 999,
                UserId = 999,
                CategoryId = 1,
                RouteId = 1,
                Title = "   Cape Town Race   ",
                Description = "   Annual running event   ",
                StartLocation = "   Cape Town Stadium   ",
                EndLocation = "   Green Point   ",
                Date = new DateTime(2026, 12, 10),
                StartTime = new TimeSpan(7, 30, 0),
                Distance = 21
            };

            // Act
            var result =
                await controller.CreateEvent(eventModel);

            // Assert
            var createdResult =
                Assert.IsType<CreatedAtActionResult>(result);

            Assert.NotNull(createdResult.Value);

            // UserId must come from JWT
            Assert.Equal(
                organiserId,
                eventModel.UserId);

            // EventId supplied by client must be reset
            Assert.NotEqual(999, eventModel.EventId);

            // Strings should be trimmed
            Assert.Equal(
                "Cape Town Race",
                eventModel.Title);

            Assert.Equal(
                "Annual running event",
                eventModel.Description);

            // Confirm database record
            var savedEvent =
                await context.Events
                    .FirstOrDefaultAsync();

            Assert.NotNull(savedEvent);

            Assert.Equal(
                organiserId,
                savedEvent.UserId);
        }


        // =========================================================
        // TEST 5
        // CREATE EVENT WITH INVALID TITLE
        // =========================================================

        [Fact]
        public async Task CreateEvent_MissingTitle_ReturnsBadRequest()
        {
            // Arrange
            using var context = CreateDbContext();

            int organiserId = 1;

            context.Organisers.Add(new Organiser
            {
                UserId = organiserId
            });

            context.Categories.Add(new Category
            {
                CategoryId = 1
            });

            context.Routes.Add(new Route
            {
                RouteId = 1
            });

            await context.SaveChangesAsync();

            var controller =
                CreateController(context, organiserId);

            var eventModel = new Event
            {
                Title = "",
                CategoryId = 1,
                RouteId = 1,
                Date = new DateTime(2026, 12, 10),
                StartTime = new TimeSpan(8, 0, 0),
                Distance = 10
            };

            // Act
            var result =
                await controller.CreateEvent(eventModel);

            // Assert
            var badRequest =
                Assert.IsType<BadRequestObjectResult>(result);

            Assert.NotNull(badRequest.Value);

            Assert.Empty(
                await context.Events.ToListAsync());
        }


        // =========================================================
        // TEST 6
        // CREATE EVENT WITH NEGATIVE DISTANCE
        // =========================================================

        [Fact]
        public async Task CreateEvent_NegativeDistance_ReturnsBadRequest()
        {
            // Arrange
            using var context = CreateDbContext();

            int organiserId = 1;

            context.Organisers.Add(new Organiser
            {
                UserId = organiserId
            });

            context.Categories.Add(new Category
            {
                CategoryId = 1
            });

            context.Routes.Add(new Route
            {
                RouteId = 1
            });

            await context.SaveChangesAsync();

            var controller =
                CreateController(context, organiserId);

            var eventModel = new Event
            {
                Title = "Test Race",
                CategoryId = 1,
                RouteId = 1,
                Date = new DateTime(2026, 12, 10),
                StartTime = new TimeSpan(8, 0, 0),
                Distance = -10
            };

            // Act
            var result =
                await controller.CreateEvent(eventModel);

            // Assert
            var badRequest =
                Assert.IsType<BadRequestObjectResult>(result);

            Assert.NotNull(badRequest.Value);
        }


        // =========================================================
        // TEST 7
        // UPDATE EVENT SUCCESSFULLY
        // =========================================================

        [Fact]
        public async Task UpdateEvent_OwnEvent_ReturnsOk()
        {
            // Arrange
            using var context = CreateDbContext();

            int organiserId = 1;

            context.Categories.Add(new Category
            {
                CategoryId = 1
            });

            context.Routes.Add(new Route
            {
                RouteId = 1
            });

            context.Events.Add(new Event
            {
                EventId = 1,
                UserId = organiserId,
                CategoryId = 1,
                RouteId = 1,
                Title = "Old Race",
                Date = new DateTime(2026, 10, 10),
                StartTime = new TimeSpan(8, 0, 0),
                Distance = 10
            });

            await context.SaveChangesAsync();

            var controller =
                CreateController(context, organiserId);

            var updatedEvent = new Event
            {
                CategoryId = 1,
                RouteId = 1,
                Title = "Updated Race",
                Description = "Updated description",
                StartLocation = "New Start",
                EndLocation = "New End",
                Date = new DateTime(2026, 12, 20),
                StartTime = new TimeSpan(9, 0, 0),
                Distance = 21
            };

            // Act
            var result =
                await controller.UpdateEvent(
                    1,
                    updatedEvent);

            // Assert
            var okResult =
                Assert.IsType<OkObjectResult>(result);

            Assert.NotNull(okResult.Value);

            var savedEvent =
                await context.Events
                    .FirstAsync(e => e.EventId == 1);

            Assert.Equal(
                "Updated Race",
                savedEvent.Title);

            Assert.Equal(
                21,
                savedEvent.Distance);

            Assert.Equal(
                organiserId,
                savedEvent.UserId);
        }


        // =========================================================
        // TEST 8
        // UPDATE SOMEONE ELSE'S EVENT
        // =========================================================

        [Fact]
        public async Task UpdateEvent_NotOwner_ReturnsForbid()
        {
            // Arrange
            using var context = CreateDbContext();

            // Event belongs to organiser 1
            context.Events.Add(new Event
            {
                EventId = 1,
                UserId = 1,
                CategoryId = 1,
                RouteId = 1,
                Title = "Private Race",
                Date = new DateTime(2026, 11, 10),
                StartTime = new TimeSpan(8, 0, 0),
                Distance = 10
            });

            await context.SaveChangesAsync();

            // Logged-in user is organiser 2
            var controller =
                CreateController(context, 2);

            var updatedEvent = new Event
            {
                CategoryId = 1,
                RouteId = 1,
                Title = "Hacked Race",
                Date = new DateTime(2026, 12, 1),
                StartTime = new TimeSpan(9, 0, 0),
                Distance = 20
            };

            // Act
            var result =
                await controller.UpdateEvent(
                    1,
                    updatedEvent);

            // Assert
            Assert.IsType<ForbidResult>(result);
        }


        // =========================================================
        // TEST 9
        // DELETE EVENT SUCCESSFULLY
        // =========================================================

        [Fact]
        public async Task DeleteEvent_OwnEventWithoutEnrolments_ReturnsOk()
        {
            // Arrange
            using var context = CreateDbContext();

            int organiserId = 1;

            context.Events.Add(new Event
            {
                EventId = 1,
                UserId = organiserId,
                CategoryId = 1,
                RouteId = 1,
                Title = "Race To Delete",
                Date = new DateTime(2026, 12, 10),
                StartTime = new TimeSpan(8, 0, 0),
                Distance = 10
            });

            await context.SaveChangesAsync();

            var controller =
                CreateController(context, organiserId);

            // Act
            var result =
                await controller.DeleteEvent(1);

            // Assert
            var okResult =
                Assert.IsType<OkObjectResult>(result);

            Assert.NotNull(okResult.Value);

            // Verify it was removed
            var deletedEvent =
                await context.Events
                    .FirstOrDefaultAsync(e => e.EventId == 1);

            Assert.Null(deletedEvent);
        }


        // =========================================================
        // TEST 10
        // DELETE SOMEONE ELSE'S EVENT
        // =========================================================

        [Fact]
        public async Task DeleteEvent_NotOwner_ReturnsForbid()
        {
            // Arrange
            using var context = CreateDbContext();

            context.Events.Add(new Event
            {
                EventId = 1,
                UserId = 1,
                CategoryId = 1,
                RouteId = 1,
                Title = "Protected Race",
                Date = new DateTime(2026, 12, 10),
                StartTime = new TimeSpan(8, 0, 0),
                Distance = 10
            });

            await context.SaveChangesAsync();

            // User 2 tries to delete user 1's event
            var controller =
                CreateController(context, 2);

            // Act
            var result =
                await controller.DeleteEvent(1);

            // Assert
            Assert.IsType<ForbidResult>(result);

            // Event must still exist
            var existingEvent =
                await context.Events
                    .FirstOrDefaultAsync(e => e.EventId == 1);

            Assert.NotNull(existingEvent);
        }
    }
}