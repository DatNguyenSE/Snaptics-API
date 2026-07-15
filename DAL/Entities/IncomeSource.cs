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

        // Số tiền mỗi lần nhận
        public decimal Amount { get; set; }

        // Loại thu nhập
        public IncomeType Type { get; set; }

        // Có lặp lại ko
        public bool IsRecurring { get; set; }

        // Weekly, Monthly, Yearly
        public IncomeFrequency Frequency { get; set; }

        // Ví nhận tiền
        public int BudgetId { get; set; }

        // Còn hoạt động ko
        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public virtual Budget Budget { get; set; } = null!;
        public virtual AppUser User { get; set; } = null!;
    }
}
