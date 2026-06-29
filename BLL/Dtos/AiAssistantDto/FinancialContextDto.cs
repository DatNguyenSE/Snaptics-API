using System;
using System.Collections.Generic;
using System.Text;

namespace BLL.Dtos.AiAssistantDto
{
    public class FinancialContextDto
    {
        public decimal TotalSpentThisMonth { get; set; }

        public int TotalTransactionsThisMonth { get; set; }

        public string? TopSpendingCategory { get; set; }

        public List<string> AllCategoriesThisMonth { get; set; } = new();

        public decimal BudgetUsagePercent { get; set; }

        public int NeedReviewCount { get; set; }

        public List<string> NeedReviewItems { get; set; } = new();

        public int MissingPriceCount { get; set; }

        public string? TopSpendingItem { get; set; }

        public decimal TopSpendingItemAmount { get; set; }

        public decimal PreviousMonthSpent { get; set; }

        public List<CategorySpendingDto> CategorySpendings { get; set; } = new();

        public List<TopExpenseDto> TopExpenses { get; set; } = new();
    }
}
