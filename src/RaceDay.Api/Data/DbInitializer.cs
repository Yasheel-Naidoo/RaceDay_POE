using RaceDay.Api.Models;
using RaceDay.Api.Services;

namespace RaceDay.Api.Data
{
    public static class DbInitializer
    {
        public static void Initialize(RaceDayDbContext context, IPasswordService passwordService)
        {
            if (context.Organisers.Any()) return;

            var o1 = new Organiser
            {
                OrganisationName = "RunSA Event Management",
                FullName = "Thabo Mokoena",
                Email = "thabo@runsa.co.za",
                Phone = "+27 82 555 0142",
                PasswordHash = passwordService.HashPassword("Password123!")
            };
            var o2 = new Organiser
            {
                OrganisationName = "Cape Cycling Promotions",
                FullName = "Sarah van der Merwe",
                Email = "sarah@capecycling.co.za",
                Phone = "+27 72 555 0198",
                PasswordHash = passwordService.HashPassword("Password123!")
            };
            context.Organisers.AddRange(o1, o2);
            context.SaveChanges();

            var p1 = new Participant
            {
                FullName = "Nomsa Dlamini",
                Email = "nomsa.dlamini@gmail.com",
                Phone = "+27 83 555 0231",
                DateOfBirth = new DateTime(1992, 4, 11),
                EmergencyContactName = "Sipho Dlamini",
                EmergencyContactPhone = "+27 83 555 0319",
                PasswordHash = passwordService.HashPassword("Password123!")
            };
            var p2 = new Participant
            {
                FullName = "Pieter Botha",
                Email = "pieter.botha@gmail.com",
                Phone = "+27 84 555 0667",
                DateOfBirth = new DateTime(1985, 9, 30),
                EmergencyContactName = "Anna Botha",
                EmergencyContactPhone = "+27 84 555 0701",
                PasswordHash = passwordService.HashPassword("Password123!")
            };
            context.Participants.AddRange(p1, p2);
            context.SaveChanges();

            var v1 = new Venue { VenueName = "FNB Stadium Precinct", Address = "Nasrec Road", City = "Johannesburg", Province = "Gauteng", PostalCode = "2001", RouteDescription = "Flat city loop." };
            var v2 = new Venue { VenueName = "Sea Point Promenade", Address = "Beach Road", City = "Cape Town", Province = "Western Cape", PostalCode = "8005", RouteDescription = "Coastal route with winds." };
            var v3 = new Venue { VenueName = "uMngeni River Park", Address = "Alexandra Road", City = "Pietermaritzburg", Province = "KwaZulu-Natal", PostalCode = "3201", RouteDescription = "Gravel trails." };
            context.Venues.AddRange(v1, v2, v3);
            context.SaveChanges();

            var ev1 = new Event { EventName = "Soweto Marathon 2026", EventDate = new DateTime(2026, 6, 14), StartTime = new TimeSpan(6, 30, 0), Status = "Closed", OrganiserID = o1.OrganiserID, VenueID = v1.VenueID, Description = "Gauteng road marathon." };
            var ev2 = new Event { EventName = "Cape Town Cycle Tour 2027", EventDate = new DateTime(2027, 3, 7), StartTime = new TimeSpan(5, 45, 0), Status = "Open", OrganiserID = o2.OrganiserID, VenueID = v2.VenueID, Description = "Peninsula cycle tour." };
            var ev3 = new Event { EventName = "uMngeni Community Walk & Trail Run", EventDate = new DateTime(2026, 10, 18), StartTime = new TimeSpan(7, 0, 0), Status = "Open", OrganiserID = o1.OrganiserID, VenueID = v3.VenueID, Description = "Charity walk." };
            context.Events.AddRange(ev1, ev2, ev3);
            context.SaveChanges();

            var c1 = new Category { EventID = ev1.EventID, CategoryName = "10km Fun Run", DistanceKm = 10.0m, EntryFee = 180, MaxParticipants = 8000 };
            var c2 = new Category { EventID = ev1.EventID, CategoryName = "21.1km Half Marathon", DistanceKm = 21.1m, EntryFee = 320, MaxParticipants = 12000 };
            var c3 = new Category { EventID = ev1.EventID, CategoryName = "42.2km Marathon", DistanceKm = 42.2m, EntryFee = 450, MaxParticipants = 8000 };
            var c4 = new Category { EventID = ev2.EventID, CategoryName = "109km Open Cycling", DistanceKm = 109.0m, EntryFee = 850, MaxParticipants = 20000 };
            var c5 = new Category { EventID = ev2.EventID, CategoryName = "42km Community Ride", DistanceKm = 42.0m, EntryFee = 350, MaxParticipants = 15000 };
            var c6 = new Category { EventID = ev3.EventID, CategoryName = "5km Community Walk", DistanceKm = 5.0m, EntryFee = 50, MaxParticipants = 3000 };
            var c7 = new Category { EventID = ev3.EventID, CategoryName = "10km Trail Run", DistanceKm = 10.0m, EntryFee = 120, MaxParticipants = 1500 };
            context.Categories.AddRange(c1, c2, c3, c4, c5, c6, c7);
            context.SaveChanges();

            var e1 = new Enrolment { ParticipantID = p1.ParticipantID, CategoryID = c3.CategoryID, BibNumber = "SM26-0342", Status = "Confirmed" };
            var e2 = new Enrolment { ParticipantID = p2.ParticipantID, CategoryID = c2.CategoryID, BibNumber = "SM26-1190", Status = "Confirmed" };
            var e3 = new Enrolment { ParticipantID = p2.ParticipantID, CategoryID = c4.CategoryID, BibNumber = "CT27-0218", Status = "Confirmed" };
            var e4 = new Enrolment { ParticipantID = p1.ParticipantID, CategoryID = c6.CategoryID, BibNumber = "UM26-0087", Status = "Confirmed" };
            var e5 = new Enrolment { ParticipantID = p2.ParticipantID, CategoryID = c7.CategoryID, BibNumber = "UM26-0112", Status = "Withdrawn" };
            context.Enrolments.AddRange(e1, e2, e3, e4, e5);
            context.SaveChanges();

            var r1 = new Result { EnrolmentID = e1.EnrolmentID, FinishTime = new TimeSpan(4, 12, 33), Position = 128, Status = "Completed" };
            var r2 = new Result { EnrolmentID = e2.EnrolmentID, FinishTime = new TimeSpan(2, 5, 47), Position = 517, Status = "Completed" };
            context.Results.AddRange(r1, r2);
            context.SaveChanges();
        }
    }
}