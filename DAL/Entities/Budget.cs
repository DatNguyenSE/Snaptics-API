using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DAL.Entities
{
    public class Budget
    {
        public int Id { get; set; }
        public string? UserId { get; set; }
        public decimal Amount { get; set; } = 0m;
        public DateTime StartDate { get; set; } = DateTime.UtcNow;
        public DateTime? EndDate { get; set; } = DateTime.UtcNow.AddYears(1);
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public virtual AppUser AppUser { get; set; }
    }
}