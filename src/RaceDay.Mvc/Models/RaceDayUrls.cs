using System;

namespace RaceDay.Mvc.Models
{
    /// <summary>
    /// Holds the API origin so views can resolve media that the API serves
    /// (uploaded banners and avatars live under the API's /uploads path).
    /// Set once at startup from configuration.
    /// </summary>
    public static class RaceDayUrls
    {
        public static string ApiBaseUrl { get; set; } = "http://localhost:5000";
    }
}
