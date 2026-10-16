using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Models;
using RaceDay.Api.Services;

namespace RaceDay.Api.Data
{
    public static class DbInitializer
    {
        public static void Initialize(RaceDayDbContext context, IPasswordService passwordService)
        {
            // Ensure schema is created before evaluating seed state
            context.Database.EnsureCreated();

            // Guard against duplicate execution
            if (context.Organisers.Any()) return;

            // 1. Seed Organisers
            var o1 = new Organiser
            {
                FullName = "Thabo Mokoena",
                Email = "thabo@runsa.co.za",
                Phone = "+27 82 555 0142",
                OrganisationName = "RunSA Event Management",
                PasswordHash = passwordService.HashPassword("Password123!"),
                CreatedAt = DateTime.UtcNow
            };

            var o2 = new Organiser
            {
                FullName = "Sarah Jenkins",
                Email = "sarah@capecycling.co.za",
                Phone = "+27 72 555 0198",
                OrganisationName = "Cape Cycling Promotions",
                PasswordHash = passwordService.HashPassword("Password123!"),
                CreatedAt = DateTime.UtcNow
            };

            context.Organisers.AddRange(o1, o2);
            context.SaveChanges();

            // 2. Seed Participants
            var p1 = new Participant
            {
                FullName = "Nomsa Dlamini",
                Email = "nomsa.dlamini@gmail.com",
                Phone = "+27 83 555 0231",
                DateOfBirth = new DateTime(1992, 4, 11),
                EmergencyContactName = "Sipho Dlamini",
                EmergencyContactPhone = "+27 83 555 0319",
                PasswordHash = passwordService.HashPassword("Password123!"),
                CreatedAt = DateTime.UtcNow
            };

            var p2 = new Participant
            {
                FullName = "Pieter Botha",
                Email = "pieter.botha@gmail.com",
                Phone = "+27 84 555 0667",
                DateOfBirth = new DateTime(1985, 9, 30),
                EmergencyContactName = "Anna Botha",
                EmergencyContactPhone = "+27 84 555 0701",
                PasswordHash = passwordService.HashPassword("Password123!"),
                CreatedAt = DateTime.UtcNow
            };

            context.Participants.AddRange(p1, p2);
            context.SaveChanges();

            // 3. Seed Venues
            var v1 = new Venue
            {
                VenueName = "FNB Stadium Precinct",
                Address = "Nasrec Road",
                City = "Johannesburg",
                Province = "Gauteng",
                PostalCode = "2001",
                RouteDescription = "Flat urban loop around Nasrec precinct."
            };

            var v2 = new Venue
            {
                VenueName = "Green Point Stadium Precinct",
                Address = "Fritz Sonnenberg Rd, Green Point",
                City = "Cape Town",
                Province = "Western Cape",
                PostalCode = "8051",
                RouteDescription = "Coastal road route with peninsula coastal winds."
            };

            var v3 = new Venue
            {
                VenueName = "uMngeni River Park",
                Address = "Alexandra Road",
                City = "Pietermaritzburg",
                Province = "KwaZulu-Natal",
                PostalCode = "3201",
                RouteDescription = "Gravel riverside trails and single-track terrain."
            };

            context.Venues.AddRange(v1, v2, v3);
            context.SaveChanges();

            // 4. Seed Events
            var ev1 = new Event
            {
                EventName = "Soweto Marathon 2026",
                EventDate = new DateTime(2026, 6, 14),
                StartTime = new TimeSpan(6, 30, 0),
                Status = "Closed",
                OrganiserID = o1.OrganiserID,
                VenueID = v1.VenueID,
                Description = "Gauteng premier road marathon passing historical Soweto landmarks.",
                CreatedAt = DateTime.UtcNow
            };

            var ev2 = new Event
            {
                EventName = "Cape Town Cycle Tour 2027",
                EventDate = new DateTime(2027, 3, 7),
                StartTime = new TimeSpan(5, 45, 0),
                Status = "Open",
                OrganiserID = o2.OrganiserID,
                VenueID = v2.VenueID,
                Description = "Scenic peninsula cycle tour around the Cape coastline.",
                CreatedAt = DateTime.UtcNow
            };

            var ev3 = new Event
            {
                EventName = "uMngeni Community Walk & Trail Run",
                EventDate = new DateTime(2026, 10, 18),
                StartTime = new TimeSpan(7, 0, 0),
                Status = "Open",
                OrganiserID = o1.OrganiserID,
                VenueID = v3.VenueID,
                Description = "Charity trail run and family walking event.",
                CreatedAt = DateTime.UtcNow
            };

            context.Events.AddRange(ev1, ev2, ev3);
            context.SaveChanges();

            // 5. Seed Categories
            var c1 = new Category { EventID = ev1.EventID, CategoryName = "10km Fun Run", DistanceKm = 10.0m, EntryFee = 180.00m, MaxParticipants = 8000 };
            var c2 = new Category { EventID = ev1.EventID, CategoryName = "21.1km Half Marathon", DistanceKm = 21.1m, EntryFee = 320.00m, MaxParticipants = 12000 };
            var c3 = new Category { EventID = ev1.EventID, CategoryName = "42.2km Marathon", DistanceKm = 42.2m, EntryFee = 450.00m, MaxParticipants = 8000 };

            var c4 = new Category { EventID = ev2.EventID, CategoryName = "109km Open Cycling", DistanceKm = 109.0m, EntryFee = 850.00m, MaxParticipants = 20000 };
            var c5 = new Category { EventID = ev2.EventID, CategoryName = "42km Community Ride", DistanceKm = 42.0m, EntryFee = 350.00m, MaxParticipants = 15000 };

            var c6 = new Category { EventID = ev3.EventID, CategoryName = "5km Community Walk", DistanceKm = 5.0m, EntryFee = 50.00m, MaxParticipants = 3000 };
            var c7 = new Category { EventID = ev3.EventID, CategoryName = "10km Trail Run", DistanceKm = 10.0m, EntryFee = 120.00m, MaxParticipants = 1500 };

            context.Categories.AddRange(c1, c2, c3, c4, c5, c6, c7);
            context.SaveChanges();

            // 6. Seed Enrolments
            // Nomsa in Soweto 42.2km (Event 1)
            var e1 = new Enrolment { ParticipantID = p1.ParticipantID, CategoryID = c3.CategoryID, BibNumber = "SM26-0342", Status = "Confirmed", EnrolmentDate = DateTime.UtcNow.AddMonths(-2) };
            // Pieter in Soweto 21.1km (Event 1)
            var e2 = new Enrolment { ParticipantID = p2.ParticipantID, CategoryID = c2.CategoryID, BibNumber = "SM26-1190", Status = "Confirmed", EnrolmentDate = DateTime.UtcNow.AddMonths(-2) };
            // Pieter in Cape Town Cycle Tour 109km (Event 2 - Sarah's Event)
            var e3 = new Enrolment { ParticipantID = p2.ParticipantID, CategoryID = c4.CategoryID, BibNumber = "CT27-0218", Status = "Confirmed", EnrolmentDate = DateTime.UtcNow.AddMonths(-1) };
            // Nomsa in uMngeni Walk (Event 3)
            var e4 = new Enrolment { ParticipantID = p1.ParticipantID, CategoryID = c6.CategoryID, BibNumber = "UM26-0087", Status = "Confirmed", EnrolmentDate = DateTime.UtcNow.AddDays(-15) };
            // Pieter in uMngeni Trail Run (Withdrawn)
            var e5 = new Enrolment { ParticipantID = p2.ParticipantID, CategoryID = c7.CategoryID, BibNumber = "UM26-0112", Status = "Withdrawn", EnrolmentDate = DateTime.UtcNow.AddDays(-10) };

            context.Enrolments.AddRange(e1, e2, e3, e4, e5);
            context.SaveChanges();

            // 7. Seed Results
            // Soweto Marathon result for Nomsa
            var r1 = new Result
            {
                EnrolmentID = e1.EnrolmentID,
                FinishTime = new TimeSpan(4, 12, 33),
                Position = 128,
                Status = "Completed",
                CreatedAt = DateTime.UtcNow
            };

            // Cape Town Cycle Tour result for Pieter (Sarah's Event 2)
            var r2 = new Result
            {
                EnrolmentID = e3.EnrolmentID,
                FinishTime = new TimeSpan(3, 14, 52),
                Position = 42,
                Status = "Completed",
                CreatedAt = DateTime.UtcNow
            };

            context.Results.AddRange(r1, r2);
            context.SaveChanges();
        }
    }
}