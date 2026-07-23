using System;
using System.Collections.Generic;
using System.Text;

namespace DAL.Entities
{
    public class BudgetIncomeSource
    {
        public int Id { get; set; }

        public int BudgetId { get; set; }

        public int IncomeSourceId { get; set; }

        // Số tiền của nguồn thu này trong budget
        public decimal Amount { get; set; }

        public virtual Budget Budget { get; set; } = null!;

        public virtual IncomeSource IncomeSource { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
