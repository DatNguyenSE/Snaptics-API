using System;

namespace BLL.Dtos
{
    public class SpendingComparisonDto
    {
        public SpendingPeriodData Week { get; set; }
        public SpendingPeriodData Month { get; set; }
        public SpendingPeriodData Year { get; set; }
    }

    public class SpendingPeriodData
    {
        public decimal CurrentAmount { get; set; }
        public decimal PreviousAmount { get; set; }
        public decimal PercentageChange { get; set; }
        public bool IsBetter { get; set; } // True if we spent less than previous period
    }
}
