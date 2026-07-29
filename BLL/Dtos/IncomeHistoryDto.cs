using System;

namespace BLL.Dtos
{
    public class IncomeHistoryDto
    {
        public int Id { get; set; }
        public int BudgetId { get; set; }
        public int? IncomeSourceId { get; set; }
        public string? IncomeSourceName { get; set; }
        public decimal Amount { get; set; }
        public DateTime ReceivedDate { get; set; }
        public string? Note { get; set; }
    }
}
