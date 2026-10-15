using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Controllers;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Models;
using RaceDay.Api.Services;
using Xunit;

namespace RaceDay.Tests
{
    public class RaceDayApiTests
    {
        private RaceDayDbContext GetDatabaseContext()
        {
            var options = new DbContextOptionsBuilder<RaceDayDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            var context = new RaceDayDbContext(options);
            context.Database.EnsureCreated();
            return context;
        }

        private static DefaultHttpContext CreateMockHttpContext(int? userId = null, string? role = null)
        {
            var context = new DefaultHttpContext();
            var session = new TestSession();
            if (userId.HasValue) session.SetInt32("UserId", userId.Value);
            if (!string.IsNullOrEmpty(role)) session.SetString("Role", role);
            context.Session = session;
            return context;
        }

        [Fact]
        public async Task Register_Participant_ReturnsCreated()
        {
            using var context = GetDatabaseContext();
            var auth = new AuthController(context, new PasswordService())
            {
                ControllerContext = new ControllerContext { HttpContext = CreateMockHttpContext() }
            };

            var dto = new RegisterRequestDto("Test Runner", "runner@test.co.za", "Secret123!", "Participant", "0820000000", null, DateTime.UtcNow.AddYears(-20));
            var response = await auth.Register(dto);

            var createdResult = Assert.IsType<ObjectResult>(response);
            Assert.Equal(StatusCodes.Status201Created, createdResult.StatusCode);
        }

        [Fact]
        public async Task Register_DuplicateEmail_ReturnsConflict()
        {
            using var context = GetDatabaseContext();
            var auth = new AuthController(context, new PasswordService())
            {
                ControllerContext = new ControllerContext { HttpContext = CreateMockHttpContext() }
            };

            var dto = new RegisterRequestDto("Test Runner", "dup@test.co.za", "Secret123!", "Participant", null, null, null);
            await auth.Register(dto);
            var response = await auth.Register(dto);

            Assert.IsType<ConflictObjectResult>(response);
        }

        [Fact]
        public async Task CreateEvent_AsOrganiser_ReturnsCreatedAtAction()
        {
            using var context = GetDatabaseContext();
            var venue = new Venue { VenueName = "Kings Park", City = "Durban", Province = "KZN" };
            context.Venues.Add(venue);
            await context.SaveChangesAsync();

            var eventsController = new EventsController(context)
            {
                ControllerContext = new ControllerContext { HttpContext = CreateMockHttpContext(userId: 1, role: "Organiser") }
            };

            var dto = new CreateEventDto("Durban 10k", DateTime.UtcNow.AddMonths(1), new TimeSpan(6, 0, 0), venue.VenueID, "10k race");
            var result = await eventsController.CreateEvent(dto);

            Assert.IsType<CreatedAtActionResult>(result);
        }

        [Fact]
        public async Task Enrol_Participant_AllocatesBibNumber()
        {
            using var context = GetDatabaseContext();
            var ev = new Event { EventName = "Test Run", Status = "Open", OrganiserID = 1, VenueID = 1 };
            context.Events.Add(ev);
            await context.SaveChangesAsync();

            var cat = new Category { EventID = ev.EventID, CategoryName = "10km", MaxParticipants = 100, EntryFee = 150 };
            context.Categories.Add(cat);
            await context.SaveChangesAsync();

            var controller = new EnrolmentsController(context)
            {
                ControllerContext = new ControllerContext { HttpContext = CreateMockHttpContext(userId: 1, role: "Participant") }
            };

            var result = await controller.Enrol(ev.EventID, new EnrolRequestDto(cat.CategoryID));

            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status201Created, objectResult.StatusCode);
            var enrolment = Assert.IsType<Enrolment>(objectResult.Value);
            Assert.StartsWith("BIB-", enrolment.BibNumber);
        }
    }

    public class TestSession : ISession
    {
        private readonly Dictionary<string, byte[]> _storage = new();
        public bool IsAvailable => true;
        public string Id => Guid.NewGuid().ToString();
        public IEnumerable<string> Keys => _storage.Keys;

        public void Clear() => _storage.Clear();
        public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Remove(string key) => _storage.Remove(key);
        public void Set(string key, byte[] value) => _storage[key] = value;
        public bool TryGetValue(string key, out byte[] value) => _storage.TryGetValue(key, out value!);
    }
}