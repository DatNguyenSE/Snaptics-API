using System;
using System.Collections.Generic;
using System.Text;
using DAL.Enums;

namespace DAL.Entities
{
    public class IncomeSource
    {
        public int Id { get; set; }

        public string UserId { get; set; } = string.Empty;

        // Lương, Thưởng, Freelance,...
        public string Name { get; set; } = string.Empty;

        // Số tiền mặc định mỗi lần nhận (đặc biệt cho nguồn thu định kỳ)
        public decimal Amount { get; set; }

        // Có lặp lại không
        public bool IsRecurring { get; set; }

        // Còn hoạt động không
        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public virtual AppUser User { get; set; } = null!;

        public virtual ICollection<BudgetIncomeSource> BudgetIncomeSources { get; set; }
            = new List<BudgetIncomeSource>();
    }
}