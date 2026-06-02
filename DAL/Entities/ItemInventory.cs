using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.Domain.Enums;
using API.Entities;
namespace DAL.Entities
{
    public class ItemInventory
    {
        public int Id { get; set; }
        
        public string UserId { get; set; }
        
        public int TransactionDetailId { get; set; }
        
        public UsageStatusType UsageStatus { get; set; } = UsageStatusType.Frequent;
        
        public DateTime? LastCheckedDate { get; set; }

        // Navigation Properties
        public virtual AppUser AppUsers { get; set; }
        public virtual TransactionDetail TransactionDetail { get; set; }
    }
}