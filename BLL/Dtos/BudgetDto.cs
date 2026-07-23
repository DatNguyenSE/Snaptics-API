using System;

namespace BLL.Dtos
{
    public class BudgetDto
    {
        public int Id { get; set; }

        public string? Name { get; set; }
        public bool IsDefault { get; set; }
        public decimal CurrentAmount { get; set; }
        public decimal Amount { get; set; }
        public DateTime StartDate { get; set; } = DateTime.Now;
        public DateTime? EndDate { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DAL.Enums.BudgetType Type { get; set; }
        public int? PreviousBudgetId { get; set; }
        public bool IsAutoRenew { get; set; } = true;
        public List<BudgetIncomeSourceDto> BudgetIncomeSources { get; set; } = [];
    }
}
