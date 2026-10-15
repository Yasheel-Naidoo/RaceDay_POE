namespace RaceDay.Api.DTOs
{
    // Authentication DTOs
    public record RegisterRequestDto(
        string FullName,
        string Email,
        string Password,
        string Role,
        string? Phone,
        string? OrganisationName,
        DateTime? DateOfBirth
    );

    public record LoginRequestDto(
        string Email,
        string Password
    );

    public record AuthResponseDto(
        int UserId,
        string FullName,
        string Email,
        string Role,
        string Message
    );

    // User Profile DTOs
    public record UserProfileDto(
        int Id,
        string FullName,
        string Email,
        string Role,
        string? Phone,
        string? OrganisationName,
        DateTime? DateOfBirth,
        string? EmergencyContactName,
        string? EmergencyContactPhone
    );

    public record UpdateProfileDto(
        string? FullName,
        string? Phone,
        string? EmergencyContactName,
        string? EmergencyContactPhone
    );

    // Event Request DTOs
    public record CreateEventDto(
        string EventName,
        DateTime EventDate,
        TimeSpan? StartTime,
        int VenueId,
        string? Description
    );

    public record UpdateEventDto(
        string? EventName,
        DateTime? EventDate,
        TimeSpan? StartTime,
        string? Description,
        string? Status
    );

    // Venue & Category Response DTOs
    public record VenueResponseDto(
        int VenueId,
        string VenueName,
        string? Address,
        string City,
        string Province,
        string? PostalCode,
        string? RouteDescription
    );

    public record CategoryResponseDto(
        int CategoryId,
        string CategoryName,
        decimal? DistanceKm,
        decimal EntryFee,
        int MaxParticipants
    );

    public record EventResponseDto(
        int EventId,
        string EventName,
        DateTime EventDate,
        TimeSpan? StartTime,
        string? Description,
        string Status,
        int OrganiserId,
        DateTime CreatedAt,
        VenueResponseDto? Venue,
        List<CategoryResponseDto> Categories
    );

    // Category Request DTOs
    public record CreateCategoryDto(
        string CategoryName,
        decimal? DistanceKm,
        decimal EntryFee,
        int MaxParticipants
    );

    public record UpdateCategoryDto(
        string? CategoryName,
        decimal? DistanceKm,
        decimal? EntryFee,
        int? MaxParticipants
    );

    // Enrolment DTOs
    public record EnrolRequestDto(
        int CategoryId
    );

    // Result Request & Response DTOs
    public record CaptureResultDto(
        int EnrolmentId,
        TimeSpan FinishTime,
        int Position,
        string Status
    );

    public record UpdateResultDto(
        TimeSpan? FinishTime,
        int? Position,
        string? Status
    );

    public record ResultResponseDto(
        int ResultId,
        int EnrolmentId,
        TimeSpan? FinishTime,
        int? Position,
        string Status,
        DateTime CreatedAt
    );

    public record MyResultResponseDto(
        int ResultId,
        int EnrolmentId,
        string EventName,
        string CategoryName,
        string BibNumber,
        TimeSpan? FinishTime,
        int? Position,
        string Status,
        DateTime CreatedAt
    );
}