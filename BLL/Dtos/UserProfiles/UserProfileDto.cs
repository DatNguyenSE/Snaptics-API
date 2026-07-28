using System;

namespace BLL.Dtos.UserProfiles
{
    public class UserProfileDto
    {
        public string Id { get; set; } = null!;
        public string? Email { get; set; }
        public string? DisplayName { get; set; }
        public string? ImageUrl { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? PostCode { get; set; }
        public string? Country { get; set; }
        public bool TrackCalories { get; set; }
        public TimeSpan? DefaultReminderTime { get; set; }
    }
}
