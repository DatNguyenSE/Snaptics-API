using System;
using System.Collections.Generic;
using System.Text;
using DAL.Enums;

namespace BLL.Dtos
{
    public class IncomeSourceDto
    {
        public int Id { get; set; }

        public string UserId { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public decimal Amount { get; set; }

        public IncomeType Type { get; set; }

        public bool IsRecurring { get; set; }

        public IncomeFrequency Frequency { get; set; }

        public int BudgetId { get; set; }

        public bool IsActive { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
