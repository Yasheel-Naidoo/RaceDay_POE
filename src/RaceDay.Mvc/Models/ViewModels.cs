using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;

namespace RaceDay.Mvc.Models
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Email address is required."), EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required."), DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;
    }

    public class RegisterViewModel
    {
        [Required, StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required, MinLength(6)]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required]
        public string Role { get; set; } = "Participant";

        [Required, Phone]
        public string Phone { get; set; } = string.Empty;

        public string? OrganisationName { get; set; }

        [DataType(DataType.Date)]
        public DateTime? DateOfBirth { get; set; }
    }

    public class UserSessionViewModel
    {
        // The API's login response uses "userId", not "id".
        [JsonPropertyName("userId")]
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
    }

    public class UpdateProfileDto
    {
        public string? FullName { get; set; }
        public string? Phone { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? EmergencyContactName { get; set; }
        public string? EmergencyContactPhone { get; set; }
    }

    public class UserProfileViewModel
    {
        public int ParticipantID { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? EmergencyContactName { get; set; }
        public string? EmergencyContactPhone { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? ProfilePictureUrl { get; set; }
    }

    public class EventSummaryViewModel
    {
        public int EventID { get; set; }
        public string EventName { get; set; } = string.Empty;
        public DateTime EventDate { get; set; }
        public TimeSpan StartTime { get; set; }
        public string Status { get; set; } = "Open";
        public string? Description { get; set; }
        public string? BannerImageUrl { get; set; }

        public int OrganiserId { get; set; }

        public int VenueId { get; set; }

        public bool IsRegistrationOpen =>
            string.Equals(Status, "Open", StringComparison.OrdinalIgnoreCase) &&
            EventDate.Date >= DateTime.UtcNow.Date;

        /// <summary>
        /// Banner URL ready for an &lt;img&gt; tag. Files uploaded through the API live under
        /// its /uploads path, so those need the API origin; everything else is served as-is.
        /// </summary>
        public string? BannerImageUrlResolved => ResolveBannerUrl(BannerImageUrl);

        public static string? ResolveBannerUrl(string? url) =>
            string.IsNullOrEmpty(url)
                ? null
                : url.StartsWith("/uploads", StringComparison.OrdinalIgnoreCase)
                    ? RaceDayUrls.ApiBaseUrl + url
                    : url;
        public string VenueName { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string Province { get; set; } = string.Empty;
        public List<CategoryViewModel> Categories { get; set; } = new();
    }

    public class CategoryViewModel
    {
        public int CategoryID { get; set; }
        public int EventID { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public decimal DistanceKm { get; set; }
        public decimal EntryFee { get; set; }
        public int MaxParticipants { get; set; }
        public int EnrolledCount { get; set; }
    }

    public class OrganiserDashboardViewModel
    {
        public int TotalEventsCreated { get; set; }
        public int TotalActiveEnrolments { get; set; }
        public int UpcomingRacesCount { get; set; }
        public List<EventSummaryViewModel> ManagedEvents { get; set; } = new();
    }

    public class CreateEventViewModel
    {
        [Required, StringLength(150)]
        public string EventName { get; set; } = string.Empty;

        [Required, DataType(DataType.Date)]
        public DateTime EventDate { get; set; } = DateTime.UtcNow.AddMonths(1);

        [Required]
        public TimeSpan StartTime { get; set; } = new TimeSpan(6, 0, 0);

        [Required]
        public int VenueID { get; set; } = 1;

        [Required, StringLength(1000)]
        public string Description { get; set; } = string.Empty;

        public IFormFile? BannerImage { get; set; }

        [MinLength(1, ErrorMessage = "Add at least one race category so athletes can enter.")]
        public List<CreateCategoryViewModel> Categories { get; set; } = new();
    }

    public class CreateCategoryViewModel
    {
        [Required, StringLength(100)]
        public string CategoryName { get; set; } = string.Empty;

        [Range(0.1, 1000, ErrorMessage = "Distance must be between 0.1 and 1000 km.")]
        public decimal DistanceKm { get; set; } = 10m;

        [Range(0, 100000, ErrorMessage = "Entry fee cannot be negative.")]
        public decimal EntryFee { get; set; }

        [Range(1, 1000000, ErrorMessage = "Capacity must be at least 1.")]
        public int MaxParticipants { get; set; } = 100;
    }

    public class EditEventViewModel
    {
        public int EventID { get; set; }

        public string? BannerImageUrl { get; set; }

        public string? BannerImageUrlResolved => EventSummaryViewModel.ResolveBannerUrl(BannerImageUrl);

        public bool RemoveBanner { get; set; }

        [Required, StringLength(150)]
        public string EventName { get; set; } = string.Empty;

        [Required, DataType(DataType.Date)]
        public DateTime EventDate { get; set; }

        [Required]
        public TimeSpan StartTime { get; set; }

        [Required]
        public int VenueID { get; set; }

        [Required, StringLength(1000)]
        public string Description { get; set; } = string.Empty;

        [Required]
        public string Status { get; set; } = "Open";
    }

    public class ParticipantEnrolmentViewModel
    {
        public int EnrolmentID { get; set; }
        public int ParticipantID { get; set; }
        public string ParticipantName { get; set; } = string.Empty;
        public int EventID { get; set; }
        public string EventName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public decimal DistanceKm { get; set; }
        public string BibNumber { get; set; } = string.Empty;
        public DateTime EnrolmentDate { get; set; }
        public string Status { get; set; } = "Confirmed";
        public DateTime EventDate { get; set; }
    }

    public class ParticipantResultViewModel
    {
        public int ResultID { get; set; }
        public int EnrolmentId { get; set; }
        public string EventName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string BibNumber { get; set; } = string.Empty;
        public TimeSpan FinishTime { get; set; }
        public int Position { get; set; }
        public string Status { get; set; } = "Completed";
    }

    public class ResultSubmissionViewModel
    {
        [Required]
        public int EnrolmentID { get; set; }

        [Required]
        public TimeSpan FinishTime { get; set; }

        [Required, Range(1, 100000)]
        public int Position { get; set; }

        [Required]
        public string Status { get; set; } = "Completed";
    }

    public class ProfileViewModel
    {
        public int ParticipantID { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public DateTime? DateOfBirth { get; set; }
        public string? EmergencyContactName { get; set; }
        public string? EmergencyContactPhone { get; set; }
        public string? ProfilePictureUrl { get; set; }
    }
}