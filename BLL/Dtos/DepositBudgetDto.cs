using System;
using System.ComponentModel.DataAnnotations;

namespace BLL.Dtos
{
    public class DepositBudgetDto
    {
        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
        public decimal Amount { get; set; }

        public int? IncomeSourceId { get; set; }

        public string? Note { get; set; }
    }
}
