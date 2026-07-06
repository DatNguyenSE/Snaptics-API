using System;
using System.Collections.Generic;

namespace BLL.Dtos
{
    public class CreateTransactionWithDetailsDto
    {
        public int? BudgetId { get; set; }
        public string? MerchantName { get; set; }
        public string? ImageKey { get; set; }
        public required string UserId { get; set; }
        public decimal TotalAmount { get; set; }
        public DateTime TransactionDate { get; set; }
        public string? Note { get; set; }

        public List<CreateTransactionDetailItemDto> Items { get; set; } = new();
    }

    public class CreateTransactionDetailItemDto
    {
        public string ItemName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public decimal Quantity { get; set; } = 1;
        public string? Category { get; set; } // The string category from AI
        public int? EstimatedCalories { get; set; }
        public string Unit { get; set; } = "cái";
    }
}
