using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Controllers;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Models;
using RaceDay.Api.Services;
using System.Diagnostics.CodeAnalysis;
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

            // Seed reference organiser
            var organiser = new Organiser
            {
                OrganiserID = 1,
                FullName = "Thabo Mokoena",
                Email = "thabo@runsa.co.za",
                PasswordHash = "hashed_pw",
                Phone = "+27 82 555 0101",
                OrganisationName = "Run South Africa",
                CreatedAt = DateTime.UtcNow
            };
            context.Organisers.Add(organiser);

            // Seed reference venue
            var venue = new Venue
            {
                VenueID = 1,
                VenueName = "FNB Stadium Precinct",
                Address = "Nasrec Road",
                City = "Johannesburg",
                Province = "Gauteng",
                PostalCode = "2001"
            };
            context.Venues.Add(venue);

            // Seed Event 1
            var ev = new Event
            {
                EventID = 1,
                EventName = "Soweto Marathon 2026",
                EventDate = DateTime.UtcNow.AddMonths(2),
                StartTime = TimeSpan.FromHours(6),
                Status = "Open",
                OrganiserID = 1,
                VenueID = 1,
                CreatedAt = DateTime.UtcNow
            };
            context.Events.Add(ev);

            // Seed Category with capacity limit of 2 participants
            var cat = new Category
            {
                CategoryID = 1,
                EventID = 1,
                CategoryName = "10km Fun Run",
                DistanceKm = 10,
                EntryFee = 180,
                MaxParticipants = 2
            };
            context.Categories.Add(cat);

            context.SaveChanges();
            return context;
        }

        private void SetFakeSession(ControllerBase controller, int userId, string role)
        {
            var httpContext = new DefaultHttpContext();
            var session = new TestSession();
            session.SetInt32("UserId", userId);
            session.SetString("Role", role);
            httpContext.Session = session;
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };
        }

        [Fact]
        public async Task Test1_PublicGetEvents_ReturnsAllEventsWithDtoProjections()
        {
            using var context = GetDatabaseContext();
            var controller = new EventsController(context);

            var result = await controller.GetEvents(null);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var events = Assert.IsAssignableFrom<IEnumerable<EventResponseDto>>(okResult.Value);
            Assert.Single(events);
        }

        [Fact]
        public async Task Test2_RegisterDuplicateEmail_Returns409Conflict()
        {
            using var context = GetDatabaseContext();
            var passwordService = new PasswordService();
            var controller = new AuthController(context, passwordService);

            var dto = new RegisterRequestDto(
                "Duplicate Applicant",
                "thabo@runsa.co.za", // Conflicts with seeded Organiser
                "Password123!",
                "Participant",
                "+27 82 000 0000",
                null,
                DateTime.UtcNow.AddYears(-22)
            );

            var result = await controller.Register(dto);

            var conflictResult = Assert.IsType<ConflictObjectResult>(result);
            Assert.Equal(StatusCodes.Status409Conflict, conflictResult.StatusCode);
        }

        [Fact]
        public async Task Test3_OrganiserCreatesEvent_BindsOwnershipAndReturns201()
        {
            using var context = GetDatabaseContext();
            var controller = new EventsController(context);
            SetFakeSession(controller, userId: 1, role: "Organiser");

            var dto = new CreateEventDto(
                "Durban City 10k Run",
                DateTime.UtcNow.AddMonths(4),
                TimeSpan.FromHours(6),
                1,
                "Fast coastal route."
            );

            var result = await controller.CreateEvent(dto);

            // Compatible with CreatedAtActionResult or StatusCode(201, ...)
            var objectResult = Assert.IsAssignableFrom<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status201Created, objectResult.StatusCode);
            var responseData = Assert.IsType<EventResponseDto>(objectResult.Value);
            Assert.Equal("Durban City 10k Run", responseData.EventName);
            Assert.Equal(1, responseData.OrganiserId);
        }

        [Fact]
        public async Task Test4_ParticipantEnrolment_AllocatesBibAndReturns201()
        {
            using var context = GetDatabaseContext();
            var controller = new EnrolmentsController(context);
            SetFakeSession(controller, userId: 10, role: "Participant");

            var dto = new EnrolRequestDto(CategoryId: 1);

            var result = await controller.Enrol(eventId: 1, dto);

            // Accommodates ObjectResult returning StatusCode 201
            var objectResult = Assert.IsAssignableFrom<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status201Created, objectResult.StatusCode);

            var enrolment = await context.Enrolments.FirstOrDefaultAsync(e => e.ParticipantID == 10);
            Assert.NotNull(enrolment);
            Assert.StartsWith("BIB-1-1-", enrolment.BibNumber);
        }

        [Fact]
        public async Task Test5_DuplicateEnrolment_Returns409Conflict()
        {
            using var context = GetDatabaseContext();
            var controller = new EnrolmentsController(context);
            SetFakeSession(controller, userId: 10, role: "Participant");

            var dto = new EnrolRequestDto(CategoryId: 1);

            // First enrolment succeeds
            await controller.Enrol(eventId: 1, dto);

            // Second enrolment by the same participant must conflict
            var duplicateResult = await controller.Enrol(eventId: 1, dto);

            var conflictResult = Assert.IsType<ConflictObjectResult>(duplicateResult);
            Assert.Equal(StatusCodes.Status409Conflict, conflictResult.StatusCode);
        }

        [Fact]
        public async Task Test6_CategoryCapacityExceeded_ReturnsCapacityConflict()
        {
            using var context = GetDatabaseContext();
            var controller = new EnrolmentsController(context);

            // Fill quota (Category 1 capacity is 2)
            SetFakeSession(controller, userId: 10, role: "Participant");
            await controller.Enrol(eventId: 1, new EnrolRequestDto(CategoryId: 1));

            SetFakeSession(controller, userId: 11, role: "Participant");
            await controller.Enrol(eventId: 1, new EnrolRequestDto(CategoryId: 1));

            // Attempting 3rd entry exceeds capacity - controller returns ConflictObjectResult (409)
            SetFakeSession(controller, userId: 12, role: "Participant");
            var thirdResult = await controller.Enrol(eventId: 1, new EnrolRequestDto(CategoryId: 1));

            var objectResult = Assert.IsAssignableFrom<ObjectResult>(thirdResult);
            // Accepts 409 Conflict or 400 BadRequest depending on controller branch
            Assert.True(
                objectResult.StatusCode == StatusCodes.Status409Conflict ||
                objectResult.StatusCode == StatusCodes.Status400BadRequest
            );
        }
    }

    public class TestSession : ISession
    {
        private readonly Dictionary<string, byte[]> _store = new();
        public bool IsAvailable => true;
        public string Id => Guid.NewGuid().ToString();
        public IEnumerable<string> Keys => _store.Keys;

        public void Clear() => _store.Clear();
        public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Remove(string key) => _store.Remove(key);
        public void Set(string key, byte[] value) => _store[key] = value;

        public bool TryGetValue(string key, [NotNullWhen(true)] out byte[]? value)
        {
            return _store.TryGetValue(key, out value);
        }
    }
}