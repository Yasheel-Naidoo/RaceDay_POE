using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Models;
using RaceDay.Api.Services;

namespace RaceDay.Api.Data
{
    public static class DbInitializer
    {
        public static void Initialize(RaceDayDbContext context, IPasswordService passwordService)
        {
            context.Database.EnsureCreated();

            if (context.Organisers.Any()) return;

            // 1. Organisers
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

            // 2. Participants
            var p1 = new Participant
            {
                FullName = "Nomsa Dlamini",
                Email = "nomsa.dlamini@gmail.com",
                Phone = "+27 83 555 0231",
                DateOfBirth = new DateTime(1992, 4, 11),
                EmergencyContactName = "Sipho Dlamini",
                EmergencyContactPhone = "+27 83 555 0319",
                ProfilePictureUrl = "https://images.unsplash.com/photo-1534528741775-53994a69daeb?w=300",
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
                ProfilePictureUrl = "https://images.unsplash.com/photo-1507003211169-0a1dd7228f2d?w=300",
                PasswordHash = passwordService.HashPassword("Password123!"),
                CreatedAt = DateTime.UtcNow
            };
            context.Participants.AddRange(p1, p2);
            context.SaveChanges();

            // 3. Venues
            var v1 = new Venue
            {
                VenueName = "FNB Stadium Precinct",
                Address = "Nasrec Road",
                City = "Johannesburg",
                Province = "Gauteng",
                PostalCode = "2001",
                RouteDescription = "Flat city loop with fast tarmac sections."
            };

            var v2 = new Venue
            {
                VenueName = "Green Point Stadium Precinct",
                Address = "Fritz Sonnenberg Rd, Green Point",
                City = "Cape Town",
                Province = "Western Cape",
                PostalCode = "8051",
                RouteDescription = "Peninsula coastal route along Atlantic coastline."
            };

            var v3 = new Venue
            {
                VenueName = "uMngeni River Park",
                Address = "Alexandra Road",
                City = "Pietermaritzburg",
                Province = "KwaZulu-Natal",
                PostalCode = "3201",
                RouteDescription = "Scenic gravel riverside trails."
            };
            context.Venues.AddRange(v1, v2, v3);
            context.SaveChanges();

            // 4. Events
            var ev1 = new Event
            {
                EventName = "Soweto Marathon 2026",
                EventDate = new DateTime(2026, 6, 14),
                StartTime = new TimeSpan(6, 30, 0),
                Status = "Closed",
                OrganiserID = o1.OrganiserID,
                VenueID = v1.VenueID,
                Description = "The People's Marathon through historical Soweto landmarks.",
                BannerImageUrl = "https://images.unsplash.com/photo-1452626038306-9aae5e071dd3?w=1200",
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
                Description = "World famous scenic cycling tour around the Cape Peninsula.",
                BannerImageUrl = "/images/cape-town-cycle-tour.jpg",
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
                Description = "Community charity trail walk and fun run.",
                BannerImageUrl = "https://images.unsplash.com/photo-1476480862126-209bfaa8edc8?w=1200",
                CreatedAt = DateTime.UtcNow
            };
            context.Events.AddRange(ev1, ev2, ev3);
            context.SaveChanges();

            // 5. Categories
            var c1 = new Category { EventID = ev1.EventID, CategoryName = "10km Fun Run", DistanceKm = 10.0m, EntryFee = 180, MaxParticipants = 8000 };
            var c2 = new Category { EventID = ev1.EventID, CategoryName = "21.1km Half Marathon", DistanceKm = 21.1m, EntryFee = 320, MaxParticipants = 12000 };
            var c3 = new Category { EventID = ev1.EventID, CategoryName = "42.2km Marathon", DistanceKm = 42.2m, EntryFee = 450, MaxParticipants = 8000 };
            var c4 = new Category { EventID = ev2.EventID, CategoryName = "109km Open Cycling", DistanceKm = 109.0m, EntryFee = 850, MaxParticipants = 20000 };
            var c5 = new Category { EventID = ev2.EventID, CategoryName = "42km Community Ride", DistanceKm = 42.0m, EntryFee = 350, MaxParticipants = 15000 };
            var c6 = new Category { EventID = ev3.EventID, CategoryName = "5km Community Walk", DistanceKm = 5.0m, EntryFee = 50, MaxParticipants = 3000 };
            var c7 = new Category { EventID = ev3.EventID, CategoryName = "10km Trail Run", DistanceKm = 10.0m, EntryFee = 120, MaxParticipants = 1500 };
            context.Categories.AddRange(c1, c2, c3, c4, c5, c6, c7);
            context.SaveChanges();

            // 6. Enrolments
            var e1 = new Enrolment { ParticipantID = p1.ParticipantID, CategoryID = c3.CategoryID, BibNumber = "SM26-0342", Status = "Confirmed", EnrolmentDate = DateTime.UtcNow.AddMonths(-2) };
            var e2 = new Enrolment { ParticipantID = p2.ParticipantID, CategoryID = c2.CategoryID, BibNumber = "SM26-1190", Status = "Confirmed", EnrolmentDate = DateTime.UtcNow.AddMonths(-2) };
            var e3 = new Enrolment { ParticipantID = p2.ParticipantID, CategoryID = c4.CategoryID, BibNumber = "CT27-0218", Status = "Confirmed", EnrolmentDate = DateTime.UtcNow.AddMonths(-1) };
            var e4 = new Enrolment { ParticipantID = p1.ParticipantID, CategoryID = c6.CategoryID, BibNumber = "UM26-0087", Status = "Confirmed", EnrolmentDate = DateTime.UtcNow.AddDays(-15) };
            var e5 = new Enrolment { ParticipantID = p2.ParticipantID, CategoryID = c7.CategoryID, BibNumber = "UM26-0112", Status = "Withdrawn", EnrolmentDate = DateTime.UtcNow.AddDays(-10) };
            context.Enrolments.AddRange(e1, e2, e3, e4, e5);
            context.SaveChanges();

            // 7. Results
            var r1 = new Result { EnrolmentID = e1.EnrolmentID, FinishTime = new TimeSpan(4, 12, 33), Position = 128, Status = "Completed", CreatedAt = DateTime.UtcNow };
            var r2 = new Result { EnrolmentID = e2.EnrolmentID, FinishTime = new TimeSpan(2, 5, 47), Position = 517, Status = "Completed", CreatedAt = DateTime.UtcNow };
            var r3 = new Result { EnrolmentID = e3.EnrolmentID, FinishTime = new TimeSpan(3, 14, 52), Position = 42, Status = "Completed", CreatedAt = DateTime.UtcNow };
            context.Results.AddRange(r1, r2, r3);
            context.SaveChanges();
        }
    }
}