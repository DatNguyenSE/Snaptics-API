using System;
using System.Collections.Generic;
using System.Text;

namespace BLL.Dtos
{
    public class BudgetIncomeSourceDto
    {
        public int Id { get; set; }
        public int BudgetId { get; set; }
        public int IncomeSourceId { get; set; }
        public decimal Amount { get; set; }
    }
}
