using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BLL.Dtos;

namespace BLL.Interfaces.IServices
{
    public interface IDashboardService
    {
        Task<DashboardResponseDto> GetDashboardSummaryAsync(string userId, DateTime fromDate, DateTime toDate);
        Task<CategorySummaryResponseDto> GetCategorySummaryAsync(string userId, DateTime fromDate, DateTime toDate);
        Task<List<BarChartDto>> GetTrendSummaryAsync(string userId, DateTime fromDate, DateTime toDate);
        Task<SpendingComparisonDto> GetSpendingComparisonAsync(string userId);
    }
}