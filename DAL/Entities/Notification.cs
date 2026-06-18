using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DAL.Entities
{
    public class Notification
    {
        public int Id { get; set; }
        public string? UserId { get; set; }
        public string? Message { get; set; }
        public bool IsRead { get; set; } = false;
        public string? Type {get; set; } 
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public int? ItemInventoryId { get; set; }

        public int? TransactionDetailId { get; set; }
        // Navigation property
        public virtual AppUser AppUser { get; set; }
    }
}