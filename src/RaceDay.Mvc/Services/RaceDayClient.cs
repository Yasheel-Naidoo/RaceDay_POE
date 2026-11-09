using Microsoft.AspNetCore.Http;
using RaceDay.Mvc.Models;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace RaceDay.Mvc.Services
{
    public interface IRaceDayApiClient
    {
        Task<(bool success, string message)> RegisterAsync(RegisterViewModel model);
        Task<(bool success, string message, string? role, int? userId, string? fullName)> LoginAsync(LoginViewModel model);
        Task LogoutAsync();
        Task<List<EventSummaryViewModel>> GetEventsAsync(string? search = null);
        Task<EventSummaryViewModel?> GetEventDetailsAsync(int eventId);
        Task<int?> CreateEventAsync(CreateEventViewModel model);
        Task<(bool success, string message)> AddCategoryAsync(int eventId, CreateCategoryViewModel model);
        Task<(bool success, string message)> UpdateEventAsync(EditEventViewModel model);
        Task<(bool success, string message)> DeleteEventAsync(int eventId);
        Task<(bool success, string message)> UpdateCategoryAsync(int categoryId, CreateCategoryViewModel model);
        Task<(bool success, string message)> DeleteCategoryAsync(int categoryId);
        Task<bool> UploadEventBannerAsync(int eventId, IFormFile file);
        Task<List<ParticipantEnrolmentViewModel>> GetEventEnrolmentsAsync(int eventId);
        Task<(bool success, string message)> EnrolAsync(int eventId, int categoryId);
        Task<List<ParticipantEnrolmentViewModel>> GetMyEnrolmentsAsync();
        Task<bool> WithdrawEnrolmentAsync(int enrolmentId);
        Task<List<ParticipantResultViewModel>> GetMyResultsAsync();
        Task<(bool success, string message)> SubmitResultAsync(ResultSubmissionViewModel result);
        Task<List<ParticipantResultViewModel>> GetEventResultsAsync(int eventId);
        Task<(bool success, string message)> ClearEventBannerAsync(int eventId);
        Task<UserProfileViewModel?> GetProfileAsync();
        Task<bool> UploadProfileAvatarAsync(IFormFile file);
        Task<(bool success, string message)> UpdateProfileAsync(int participantId, UpdateProfileDto dto, IFormFile? profilePicture);
    }

    public class RaceDayClient : IRaceDayApiClient
    {
        private readonly HttpClient _client;

        public RaceDayClient(HttpClient client)
        {
            _client = client;
        }

        public async Task<(bool success, string message)> RegisterAsync(RegisterViewModel model)
        {
            var response = await _client.PostAsJsonAsync("api/auth/register", model);
            var content = await response.Content.ReadAsStringAsync();
            return (response.IsSuccessStatusCode, content);
        }

        public async Task<(bool success, string message, string? role, int? userId, string? fullName)> LoginAsync(LoginViewModel model)
        {
            var response = await _client.PostAsJsonAsync("api/auth/login", model);
            var content = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var user = await response.Content.ReadFromJsonAsync<UserSessionViewModel>();
                return (true, "Login successful", user?.Role, user?.Id, user?.FullName);
            }

            return (false, content, null, null, null);
        }

        public async Task LogoutAsync() => await Task.CompletedTask;

        public async Task<List<EventSummaryViewModel>> GetEventsAsync(string? search = null)
        {
            var url = string.IsNullOrEmpty(search) ? "api/events" : $"api/events?search={search}";
            return await _client.GetFromJsonAsync<List<EventSummaryViewModel>>(url) ?? new();
        }

        public async Task<EventSummaryViewModel?> GetEventDetailsAsync(int eventId) =>
            await _client.GetFromJsonAsync<EventSummaryViewModel>($"api/events/{eventId}");

        public async Task<int?> CreateEventAsync(CreateEventViewModel model)
        {
            var response = await _client.PostAsJsonAsync("api/events", model);
            if (!response.IsSuccessStatusCode) return null;

            // The API responds 201 with the full EventResponseDto, not a bare int.
            var created = await response.Content.ReadFromJsonAsync<EventSummaryViewModel>();
            return created?.EventID;
        }

        public async Task<bool> UploadEventBannerAsync(int eventId, IFormFile file)
        {
            using var content = new MultipartFormDataContent();
            using var streamContent = new StreamContent(file.OpenReadStream());
            content.Add(streamContent, "file", file.FileName);

            var response = await _client.PostAsync($"api/media/upload-event-banner/{eventId}", content);
            return response.IsSuccessStatusCode;
        }

        public async Task<List<ParticipantEnrolmentViewModel>> GetEventEnrolmentsAsync(int eventId) =>
            await _client.GetFromJsonAsync<List<ParticipantEnrolmentViewModel>>($"api/events/{eventId}/enrolments") ?? new();

        public async Task<(bool success, string message)> EnrolAsync(int eventId, int categoryId)
        {
            // The API binds EnrolRequestDto from the request body, so the category must be JSON.
            var response = await _client.PostAsJsonAsync($"api/events/{eventId}/enrol", new { categoryId });

            if (response.IsSuccessStatusCode)
                return (true, "Successfully enrolled in the race!");

            return (false, await ReadErrorMessageAsync(response, $"Failed to enrol in the race ({(int)response.StatusCode})."));
        }

        public async Task<(bool success, string message)> AddCategoryAsync(int eventId, CreateCategoryViewModel model)
        {
            var response = await _client.PostAsJsonAsync($"api/events/{eventId}/categories", new
            {
                model.CategoryName,
                model.DistanceKm,
                model.EntryFee,
                model.MaxParticipants
            });

            return response.IsSuccessStatusCode
                ? (true, "Category added.")
                : (false, await ReadErrorMessageAsync(response, $"Could not add category '{model.CategoryName}'."));
        }

        public async Task<(bool success, string message)> UpdateEventAsync(EditEventViewModel model)
        {
            var payload = new
            {
                model.EventName,
                model.EventDate,
                model.StartTime,
                VenueId = model.VenueID,
                model.Description,
                model.Status
            };

            var response = await _client.PutAsJsonAsync($"api/events/{model.EventID}", payload);
            return response.IsSuccessStatusCode
                ? (true, "Event updated successfully.")
                : (false, await ReadErrorMessageAsync(response, "Failed to update the event."));
        }

        public async Task<(bool success, string message)> DeleteEventAsync(int eventId)
        {
            var response = await _client.DeleteAsync($"api/events/{eventId}");
            return response.IsSuccessStatusCode
                ? (true, "Event deleted.")
                : (false, await ReadErrorMessageAsync(response, "Failed to delete the event."));
        }

        public async Task<(bool success, string message)> UpdateCategoryAsync(int categoryId, CreateCategoryViewModel model)
        {
            var payload = new
            {
                model.CategoryName,
                model.DistanceKm,
                model.EntryFee,
                model.MaxParticipants
            };

            var response = await _client.PutAsJsonAsync($"api/categories/{categoryId}", payload);
            return response.IsSuccessStatusCode
                ? (true, "Category updated.")
                : (false, await ReadErrorMessageAsync(response, "Failed to update the category."));
        }

        public async Task<(bool success, string message)> DeleteCategoryAsync(int categoryId)
        {
            var response = await _client.DeleteAsync($"api/categories/{categoryId}");
            return response.IsSuccessStatusCode
                ? (true, "Category deleted.")
                : (false, await ReadErrorMessageAsync(response, "Failed to delete the category."));
        }

        private static async Task<string> ReadErrorMessageAsync(HttpResponseMessage response, string fallback)
        {
            try
            {
                var error = await response.Content.ReadFromJsonAsync<ApiMessage>();
                if (!string.IsNullOrWhiteSpace(error?.Message))
                    return error!.Message!;
            }
            catch { }

            return fallback;
        }

        private record ApiMessage(string? Message);

        public async Task<List<ParticipantEnrolmentViewModel>> GetMyEnrolmentsAsync() =>
            await _client.GetFromJsonAsync<List<ParticipantEnrolmentViewModel>>("api/participants/enrolments") ?? new();

        public async Task<bool> WithdrawEnrolmentAsync(int enrolmentId)
        {
            var response = await _client.DeleteAsync($"api/enrolments/{enrolmentId}");
            return response.IsSuccessStatusCode;
        }

        public async Task<List<ParticipantResultViewModel>> GetMyResultsAsync() =>
            await _client.GetFromJsonAsync<List<ParticipantResultViewModel>>("api/results/me") ?? new();

        public async Task<(bool success, string message)> SubmitResultAsync(ResultSubmissionViewModel result)
        {
            var response = await _client.PostAsJsonAsync("api/results", result);
            return response.IsSuccessStatusCode
                ? (true, "Result recorded successfully.")
                : (false, await ReadErrorMessageAsync(response, "Failed to record the result."));
        }

        public async Task<List<ParticipantResultViewModel>> GetEventResultsAsync(int eventId) =>
            await _client.GetFromJsonAsync<List<ParticipantResultViewModel>>($"api/events/{eventId}/results") ?? new();

        public async Task<(bool success, string message)> ClearEventBannerAsync(int eventId)
        {
            var response = await _client.DeleteAsync($"api/media/event-banner/{eventId}");
            return response.IsSuccessStatusCode
                ? (true, "Event banner removed.")
                : (false, await ReadErrorMessageAsync(response, "Failed to remove the banner."));
        }

        public async Task<UserProfileViewModel?> GetProfileAsync()
        {
            try
            {
                var response = await _client.GetAsync("api/participants/profile");
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<UserProfileViewModel>();
                }
            }
            catch { }

            return new UserProfileViewModel
            {
                FullName = "Local User",
                Email = "user@raceday.local",
                Phone = "0821234567",
                EmergencyContactName = "Emergency Contact",
                EmergencyContactPhone = "0827654321"
            };
        }

        public async Task<bool> UploadProfileAvatarAsync(IFormFile file)
        {
            using var content = new MultipartFormDataContent();
            using var streamContent = new StreamContent(file.OpenReadStream());
            content.Add(streamContent, "file", file.FileName);

            var response = await _client.PostAsync("api/participants/avatar", content);
            return response.IsSuccessStatusCode;
        }

        public async Task<(bool success, string message)> UpdateProfileAsync(int participantId, UpdateProfileDto dto, IFormFile? profilePicture)
        {
            var content = new MultipartFormDataContent();
            content.Add(new StringContent(dto.FullName ?? ""), "FullName");
            content.Add(new StringContent(dto.Phone ?? ""), "Phone");
            if (dto.DateOfBirth.HasValue)
                content.Add(new StringContent(dto.DateOfBirth.Value.ToString("yyyy-MM-dd")), "DateOfBirth");
            content.Add(new StringContent(dto.EmergencyContactName ?? ""), "EmergencyContactName");
            content.Add(new StringContent(dto.EmergencyContactPhone ?? ""), "EmergencyContactPhone");

            if (profilePicture != null && profilePicture.Length > 0)
            {
                var fileStreamContent = new StreamContent(profilePicture.OpenReadStream());
                content.Add(fileStreamContent, "profilePicture", profilePicture.FileName);
            }

            var response = await _client.PutAsync($"api/users/{participantId}/profile", content);
            var responseString = await response.Content.ReadAsStringAsync();
            return (response.IsSuccessStatusCode, responseString);
        }
    }
}