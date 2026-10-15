using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDay.Api.Models
{
    public class Organiser
    {
        [Key]
        public int OrganiserID { get; set; }

        [Required, MaxLength(150)]
        public string OrganisationName { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required, MaxLength(150)]
        public string Email { get; set; } = string.Empty;

        [MaxLength(20)]
        public string? Phone { get; set; }

        [Required, MaxLength(255)]
        public string PasswordHash { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<Event> Events { get; set; } = new List<Event>();
    }

    public class Participant
    {
        [Key]
        public int ParticipantID { get; set; }

        [Required, MaxLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required, MaxLength(150)]
        public string Email { get; set; } = string.Empty;

        [MaxLength(20)]
        public string? Phone { get; set; }

        public DateTime? DateOfBirth { get; set; }

        [MaxLength(100)]
        public string? EmergencyContactName { get; set; }

        [MaxLength(20)]
        public string? EmergencyContactPhone { get; set; }

        [Required, MaxLength(255)]
        public string PasswordHash { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<Enrolment> Enrolments { get; set; } = new List<Enrolment>();
    }

    public class Venue
    {
        [Key]
        public int VenueID { get; set; }

        [Required, MaxLength(150)]
        public string VenueName { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? Address { get; set; }

        [Required, MaxLength(100)]
        public string City { get; set; } = string.Empty;

        [Required, MaxLength(50)]
        public string Province { get; set; } = string.Empty;

        [MaxLength(10)]
        public string? PostalCode { get; set; }

        public string? RouteDescription { get; set; }

        public ICollection<Event> Events { get; set; } = new List<Event>();
    }

    public class Event
    {
        [Key]
        public int EventID { get; set; }

        [Required, MaxLength(150)]
        public string EventName { get; set; } = string.Empty;

        public DateTime EventDate { get; set; }

        public TimeSpan? StartTime { get; set; }

        public string? Description { get; set; }

        [Required, MaxLength(20)]
        public string Status { get; set; } = "Open";

        public int OrganiserID { get; set; }
        public Organiser? Organiser { get; set; }

        public int VenueID { get; set; }
        public Venue? Venue { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<Category> Categories { get; set; } = new List<Category>();
    }

    public class Category
    {
        [Key]
        public int CategoryID { get; set; }

        public int EventID { get; set; }
        public Event? Event { get; set; }

        [Required, MaxLength(50)]
        public string CategoryName { get; set; } = string.Empty;

        [Column(TypeName = "decimal(5,1)")]
        public decimal? DistanceKm { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal EntryFee { get; set; } = 0;

        public int MaxParticipants { get; set; }

        public ICollection<Enrolment> Enrolments { get; set; } = new List<Enrolment>();
    }

    public class Enrolment
    {
        [Key]
        public int EnrolmentID { get; set; }

        public int ParticipantID { get; set; }
        public Participant? Participant { get; set; }

        public int CategoryID { get; set; }
        public Category? Category { get; set; }

        public DateTime EnrolmentDate { get; set; } = DateTime.UtcNow;

        [Required, MaxLength(20)]
        public string BibNumber { get; set; } = string.Empty;

        [Required, MaxLength(20)]
        public string Status { get; set; } = "Confirmed";

        public Result? Result { get; set; }
    }

    public class Result
    {
        [Key]
        public int ResultID { get; set; }

        public int EnrolmentID { get; set; }
        public Enrolment? Enrolment { get; set; }

        public TimeSpan? FinishTime { get; set; }

        public int? Position { get; set; }

        [Required, MaxLength(20)]
        public string Status { get; set; } = "Completed";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}