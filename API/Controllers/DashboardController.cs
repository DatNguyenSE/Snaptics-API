using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using API.Extensions;
using BLL.Interfaces.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DashboardController : ControllerBase
    {
        private readonly IDashboardService _dashboardService;

        public DashboardController(IDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        [HttpGet("summary")]
        public async Task<IActionResult> GetSummary(
            [FromQuery] string filterType = "month",
            [FromQuery] int? day = null, 
            [FromQuery] int? month = null, 
            [FromQuery] int? year = null)
        {

            var userId = User.GetUserId();
        
            DateTime fromDate;
            DateTime toDate;
            DateTime now = DateTime.Now;

            switch (filterType.ToLower())
            {
                case "week":
                    // Start of week (Monday)
                    int diff = (7 + (now.DayOfWeek - DayOfWeek.Monday)) % 7;
                    fromDate = now.Date.AddDays(-1 * diff);
                    toDate = fromDate.AddDays(7).AddTicks(-1);
                    break;
                case "year":
                    fromDate = new DateTime(year ?? now.Year, 1, 1);
                    toDate = fromDate.AddYears(1).AddTicks(-1);
                    break;
                case "day":
                    fromDate = new DateTime(year ?? now.Year, month ?? now.Month, day ?? now.Day);
                    toDate = fromDate.AddDays(1).AddTicks(-1);
                    break;
                case "month":
                default:
                    fromDate = new DateTime(year ?? now.Year, month ?? now.Month, 1);
                    toDate = fromDate.AddMonths(1).AddTicks(-1);
                    break;
            }

            var result = await _dashboardService.GetDashboardSummaryAsync(userId, fromDate, toDate);
            return Ok(result);
        }

        [HttpGet("category-summary")]
        public async Task<IActionResult> GetCategorySummary(
            [FromQuery] string filterType = "month",
            [FromQuery] int? day = null, 
            [FromQuery] int? month = null, 
            [FromQuery] int? year = null)
        {
            var userId = User.GetUserId();
        
            DateTime fromDate;
            DateTime toDate;
            DateTime now = DateTime.Now;

            switch (filterType.ToLower())
            {
                case "week":
                    int diff = (7 + (now.DayOfWeek - DayOfWeek.Monday)) % 7;
                    fromDate = now.Date.AddDays(-1 * diff);
                    toDate = fromDate.AddDays(7).AddTicks(-1);
                    break;
                case "year":
                    fromDate = new DateTime(year ?? now.Year, 1, 1);
                    toDate = fromDate.AddYears(1).AddTicks(-1);
                    break;
                case "day":
                    fromDate = new DateTime(year ?? now.Year, month ?? now.Month, day ?? now.Day);
                    toDate = fromDate.AddDays(1).AddTicks(-1);
                    break;
                case "month":
                default:
                    fromDate = new DateTime(year ?? now.Year, month ?? now.Month, 1);
                    toDate = fromDate.AddMonths(1).AddTicks(-1);
                    break;
            }

            var result = await _dashboardService.GetCategorySummaryAsync(userId, fromDate, toDate);
            return Ok(result);
        }

        [HttpGet("trend-summary")]
        public async Task<IActionResult> GetTrendSummary(
            [FromQuery] string filterType = "month",
            [FromQuery] int? day = null, 
            [FromQuery] int? month = null, 
            [FromQuery] int? year = null)
        {
            var userId = User.GetUserId();
        
            DateTime fromDate;
            DateTime toDate;
            DateTime now = DateTime.Now;

            switch (filterType.ToLower())
            {
                case "week":
                    int diff = (7 + (now.DayOfWeek - DayOfWeek.Monday)) % 7;
                    fromDate = now.Date.AddDays(-1 * diff);
                    toDate = fromDate.AddDays(7).AddTicks(-1);
                    break;
                case "year":
                    fromDate = new DateTime(year ?? now.Year, 1, 1);
                    toDate = fromDate.AddYears(1).AddTicks(-1);
                    break;
                case "day":
                    fromDate = new DateTime(year ?? now.Year, month ?? now.Month, day ?? now.Day);
                    toDate = fromDate.AddDays(1).AddTicks(-1);
                    break;
                case "month":
                default:
                    fromDate = new DateTime(year ?? now.Year, month ?? now.Month, 1);
                    toDate = fromDate.AddMonths(1).AddTicks(-1);
                    break;
            }

            var result = await _dashboardService.GetTrendSummaryAsync(userId, fromDate, toDate);
            return Ok(result);
        }

        [HttpGet("spending-comparison")]
        public async Task<IActionResult> GetSpendingComparison()
        {
            var userId = User.GetUserId();
            var result = await _dashboardService.GetSpendingComparisonAsync(userId);
            return Ok(result);
        }
    }
}