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

        public decimal BudgetUsagePercent { get; set; }

        public int NeedReviewCount { get; set; }

        public List<string> NeedReviewItems { get; set; } = new();

        public int MissingPriceCount { get; set; }
    }
}
