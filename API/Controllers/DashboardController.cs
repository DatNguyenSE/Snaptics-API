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

    public class DashboardController : ControllerBase
    {
        private readonly IDashboardService _dashboardService;

        public DashboardController(IDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        [HttpGet("summary")]
        public async Task<IActionResult> GetSummary(
            [FromQuery] int? day, 
            [FromQuery] int? month, 
            [FromQuery] int? year)
        {

            var userId = User.GetUserId();
        
            int filterYear = year ?? DateTime.Now.Year;
            int filterMonth = month ?? DateTime.Now.Month;

            DateTime fromDate;
            DateTime toDate;

            if (day.HasValue)
            {
                // Kịch bản A: Lọc theo ĐÚNG 1 ngày cụ thể
                fromDate = new DateTime(filterYear, filterMonth, day.Value);
                toDate = fromDate.AddDays(1).AddTicks(-1); // Lấy đến 23:59:59 của ngày đó
            }
            else
            {
                // Kịch bản B: Lọc theo NGUYÊN THÁNG
                fromDate = new DateTime(filterYear, filterMonth, 1);
                toDate = fromDate.AddMonths(1).AddTicks(-1); // Lấy đến 23:59:59 của ngày cuối tháng
            }
            var result = await _dashboardService.GetDashboardSummaryAsync(userId, fromDate, toDate);
            return Ok(result);
        }
    }
}