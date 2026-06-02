using DAL.Entities;
using Microsoft.AspNetCore.Identity;

namespace API.Entities
{
    public class AppUser : IdentityUser 
    {
        public string? FullName { get; set; }

        public bool TrackCalories { get; set; } = false;
        
        public TimeSpan? DefaultReminderTime { get; set; } = new TimeSpan(20, 0, 0); // Mặc định 20:00
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
          
        // Auth
        public string? RefreshToken { get; set; }
        public DateTime? RefreshTokenExpiry { get; set; }
    
        // Navigation Properties 
        public virtual ICollection<DAL.Entities.Transaction> Transactions { get; set; } = new List<Transaction>();
        public virtual ICollection<ItemInventory> ItemInventories { get; set; } = new List<ItemInventory>();
    }
}