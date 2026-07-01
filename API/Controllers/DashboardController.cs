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
        public async Task<IActionResult> GetSummary([FromQuery] DateTime fromDate, [FromQuery] DateTime toDate)
        {
            try
            {
                // var userId = User.GetUserId(); 
                var userId = "user-123";
                
                var result = await _dashboardService.GetDashboardSummaryAsync(userId, fromDate, toDate);
                return Ok(result);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "An error occurred while fetching dashboard summary.");
                return StatusCode(500, "An error occurred while processing your request.");
            }


        }   
    }
}