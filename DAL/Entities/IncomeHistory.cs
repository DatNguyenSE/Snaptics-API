using System;
using System.Collections.Generic;
using System.Text;

namespace DAL.Entities
{
    public class IncomeHistory
    {
        public int Id { get; set; }

        public int IncomeSourceId { get; set; }

        public decimal Amount { get; set; }

        public DateTime ReceivedDate { get; set; }

        public string? Note { get; set; }

        public virtual IncomeSource IncomeSource { get; set; } = null!;
    }
}
