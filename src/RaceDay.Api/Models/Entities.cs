using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDay.Api.Models
{
    public class Organiser
    {
        [Key]
        public int OrganiserID { get; set; }

        [Required, MaxLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required, EmailAddress, MaxLength(150)]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        [Required, Phone, MaxLength(20)]
        public string Phone { get; set; } = string.Empty;

        [MaxLength(150)]
        public string OrganisationName { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<Event> Events { get; set; } = new List<Event>();
    }

    public class Participant
    {
        [Key]
        public int ParticipantID { get; set; }

        [Required, MaxLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required, EmailAddress, MaxLength(150)]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        [Required, Phone, MaxLength(20)]
        public string Phone { get; set; } = string.Empty;

        public DateTime? DateOfBirth { get; set; }

        [MaxLength(100)]
        public string? EmergencyContactName { get; set; }

        [MaxLength(20)]
        public string? EmergencyContactPhone { get; set; }

        [MaxLength(500)]
        public string? ProfilePictureUrl { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<Enrolment> Enrolments { get; set; } = new List<Enrolment>();
    }

    public class Venue
    {
        [Key]
        public int VenueID { get; set; }

        [Required, MaxLength(150)]
        public string VenueName { get; set; } = string.Empty;

        [Required, MaxLength(255)]
        public string Address { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string City { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string Province { get; set; } = string.Empty;

        [Required, MaxLength(20)]
        public string PostalCode { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? RouteDescription { get; set; }

        public ICollection<Event> Events { get; set; } = new List<Event>();
    }

    public class Event
    {
        [Key]
        public int EventID { get; set; }

        [Required, MaxLength(150)]
        public string EventName { get; set; } = string.Empty;

        [Required]
        public DateTime EventDate { get; set; }

        [Required]
        public TimeSpan StartTime { get; set; }

        [Required, MaxLength(50)]
        public string Status { get; set; } = "Open";

        [MaxLength(1000)]
        public string? Description { get; set; }

        [MaxLength(500)]
        public string? BannerImageUrl { get; set; }

        [Required]
        public int OrganiserID { get; set; }
        [ForeignKey(nameof(OrganiserID))]
        public Organiser Organiser { get; set; } = null!;

        [Required]
        public int VenueID { get; set; }
        [ForeignKey(nameof(VenueID))]
        public Venue Venue { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<Category> Categories { get; set; } = new List<Category>();
    }

    public class Category
    {
        [Key]
        public int CategoryID { get; set; }

        [Required]
        public int EventID { get; set; }
        [ForeignKey(nameof(EventID))]
        public Event Event { get; set; } = null!;

        [Required, MaxLength(100)]
        public string CategoryName { get; set; } = string.Empty;

        [Column(TypeName = "decimal(6,2)")]
        public decimal DistanceKm { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal EntryFee { get; set; }

        public int MaxParticipants { get; set; }

        public ICollection<Enrolment> Enrolments { get; set; } = new List<Enrolment>();
    }

    public class Enrolment
    {
        [Key]
        public int EnrolmentID { get; set; }

        [Required]
        public int ParticipantID { get; set; }
        [ForeignKey(nameof(ParticipantID))]
        public Participant Participant { get; set; } = null!;

        [Required]
        public int CategoryID { get; set; }
        [ForeignKey(nameof(CategoryID))]
        public Category Category { get; set; } = null!;

        [Required, MaxLength(50)]
        public string BibNumber { get; set; } = string.Empty;

        public DateTime EnrolmentDate { get; set; } = DateTime.UtcNow;

        [Required, MaxLength(50)]
        public string Status { get; set; } = "Confirmed";

        public Result? Result { get; set; }
    }

    public class Result
    {
        [Key]
        public int ResultID { get; set; }

        [Required]
        public int EnrolmentID { get; set; }
        [ForeignKey(nameof(EnrolmentID))]
        public Enrolment Enrolment { get; set; } = null!;

        [Required]
        public TimeSpan FinishTime { get; set; }

        public int Position { get; set; }

        [Required, MaxLength(50)]
        public string Status { get; set; } = "Completed";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}