using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BLL.Dtos
{
    public class DeductMoneyDto
    {
        public decimal Amount { get; set; }
        public string Note { get; set; }
        public int? BudgetId { get; set; } // Null thì tự trừ ví mặc định
        public bool IsAiEstimated { get; set; } = false;
    }
}