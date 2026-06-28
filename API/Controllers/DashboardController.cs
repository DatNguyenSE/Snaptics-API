using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
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
        public async Task<IActionResult> GetSummary([FromQuery] DateTime fromDate, [FromQuery] DateTime toDate)
        {
            // // Lấy ID của người dùng từ Token JWT
            // var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            // if (string.IsNullOrEmpty(userId))
            // {
            //     return Unauthorized("Không tìm thấy thông tin người dùng.");
            // }

            // var data = await _dashboardService.GetDashboardSummaryAsync(userId, fromDate, toDate);
            // return Ok(data);

            var userId = "user-123";
        
            var result = await _dashboardService.GetDashboardSummaryAsync(userId, fromDate, toDate);
            return Ok(result);
        }
    }
}