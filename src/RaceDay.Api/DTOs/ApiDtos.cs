namespace RaceDay.Api.DTOs
{
    public record RegisterRequestDto(
        string FullName,
        string Email,
        string Password,
        string Role,
        string Phone,
        string? OrganisationName,
        DateTime? DateOfBirth
    );

    public record LoginRequestDto(
        string Email,
        string Password
    );

    public record CreateEventDto(
        string EventName,
        DateTime EventDate,
        TimeSpan StartTime,
        int VenueId,
        string? Description
    );

    public record UpdateEventDto(
        string EventName,
        DateTime EventDate,
        TimeSpan StartTime,
        int VenueId,
        string? Description,
        string Status
    );

    public record CreateCategoryDto(
        string CategoryName,
        decimal DistanceKm,
        decimal EntryFee,
        int MaxParticipants
    );

    public record UpdateCategoryDto(
        string CategoryName,
        decimal DistanceKm,
        decimal EntryFee,
        int MaxParticipants
    );

    public record EnrolRequestDto(
        int CategoryId
    );

    public record ResultSubmissionDto(
        int EnrolmentId,
        TimeSpan FinishTime,
        int Position,
        string Status
    );

    public record UpdateProfileDto(
        string FullName,
        string Phone,
        DateTime? DateOfBirth,
        string? EmergencyContactName,
        string? EmergencyContactPhone
    );

    // Response DTOs
    public record EventResponseDto(
        int EventID,
        string EventName,
        DateTime EventDate,
        TimeSpan StartTime,
        string Status,
        string? Description,
        string? BannerImageUrl,
        int OrganiserId,
        string OrganiserName,
        int VenueId,
        string VenueName,
        string City,
        string Province,
        List<CategoryResponseDto> Categories
    );

    public record CategoryResponseDto(
        int CategoryID,
        int EventID,
        string CategoryName,
        decimal DistanceKm,
        decimal EntryFee,
        int MaxParticipants,
        int EnrolledCount
    );

    public record EnrolmentResponseDto(
        int EnrolmentID,
        int ParticipantID,
        string ParticipantName,
        int CategoryID,
        string CategoryName,
        decimal DistanceKm,
        int EventID,
        string EventName,
        DateTime EventDate,
        string BibNumber,
        string Status,
        DateTime EnrolmentDate
    );

    public record ResultResponseDto(
        int ResultID,
        int EnrolmentID,
        string ParticipantName,
        string BibNumber,
        string EventName,
        string CategoryName,
        TimeSpan FinishTime,
        int Position,
        string Status
    );

    // Fully aligned with UsersController.cs argument types and order
    public record UserProfileDto(
        int ParticipantID,
        string FullName,
        string Email,
        string Phone,
        string? EmergencyContactName,
        string? EmergencyContactPhone,
        DateTime? DateOfBirth,
        string? ProfilePictureUrl,
        object CreatedAt
    );
}