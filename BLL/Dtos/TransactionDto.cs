using System;
using System.Collections.Generic;
using System.Text;
using DAL.Enums;

namespace BLL.Dtos
{
    public class TransactionDto
    {
        public int Id { get; set; }
        public int BudgetId { get; set; }
        public string? Name { get; set; }
        public required string UserId { get; set; }
        public string? ImageKey { get; set; }
        public decimal TotalAmount { get; set; }
        public DateTime TransactionDate { get; set; }
        public TransactionStatusType Status { get; set; }
        public bool IsAiEstimated { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? Note { get; set; }

        public List<TransactionDetailDto> TransactionDetails { get; set; } = new List<TransactionDetailDto>();
    }
}
