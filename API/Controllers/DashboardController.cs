using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using API.Extensions;
using BLL.Interfaces.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]

    public class DashboardController : BaseController<DashboardController>
    {
        private readonly IDashboardService _dashboardService;

        public DashboardController(IDashboardService dashboardService, ILogger<DashboardController> logger)
        {
            _dashboardService = dashboardService;
        }

        [HttpGet("summary")]
        public async Task<IActionResult> GetSummary(
            [FromQuery] int? day, 
            [FromQuery] int? month, 
            [FromQuery] int? year)
        {
            try
            {
                // var userId = User.GetUserId(); 
                var userId = "user-123";
                
                int filterYear = year ?? DateTime.Now.Year;
                int filterMonth = month ?? DateTime.Now.Month;
                
                DateTime fromDate;
                DateTime toDate;

                if (day.HasValue)
                {
                    // Filter trọn vẹn 1 ngày
                    fromDate = new DateTime(filterYear, filterMonth, day.Value);
                    toDate = fromDate.AddDays(1).AddTicks(-1); 
                }
                else
                {
                    // Filter trọn vẹn 1 tháng
                    fromDate = new DateTime(filterYear, filterMonth, 1);
                    toDate = fromDate.AddMonths(1).AddTicks(-1);
                }

                var result = await _dashboardService.GetDashboardSummaryAsync(userId, fromDate, toDate);
                return Ok(result);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Đã xảy ra lỗi hệ thống khi lấy dữ liệu Dashboard Summary.");
                
                return StatusCode(500, new { message = "An error occurred while processing your request." });
            }


        }   
    }
}