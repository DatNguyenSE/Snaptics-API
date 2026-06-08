using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DAL.Enums;

namespace DAL.Entities
{
    public class ItemInventory
    {
        public int Id { get; set; }
        
        public string? UserId { get; set; }
        
        public int TransactionDetailId { get; set; }

// ---    in future
        public DateTime? ManufactureDate { get; set; } = null;
        public DateTime? ExpiryDate { get; set; } = null;
        public string? UsageFeedback { get; set; } = null;
//---
        
        public bool IsReviewed { get; set; } = false;
        public DateTime? LastReviewDate { get; set; }
        public UsageStatusType UsageStatus { get; set; } = UsageStatusType.Frequent;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        

        // Navigation Properties
        public virtual AppUser AppUsers { get; set; }
        public virtual TransactionDetail TransactionDetail { get; set; }
    }
}