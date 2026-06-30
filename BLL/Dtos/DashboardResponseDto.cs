using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BLL.Dtos
{
    public class DashboardResponseDto
    {
        public decimal TotalIncome { get; set; }
        public decimal TotalExpense { get; set; }
        public decimal Balance { get; set; }
        public List<PieChartDto> PieChart { get; set; } = new();
        public List<BarChartDto> BarChart { get; set; } = new();
    }

    public class PieChartDto
    {
        public string CategoryName { get; set; }
        public decimal TotalAmount { get; set; }
    }

    public class BarChartDto
    {
        public string Label { get; set; }
        public decimal Income { get; set; }
        public decimal Expense { get; set; }
    }
}