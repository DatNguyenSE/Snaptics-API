using BLL.Dtos;
using BLL.Interfaces.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [ApiController]
    [Route("api/admin/system")]
    [Authorize(Roles = "admin,Admin")]
    public class AdminSystemController : ControllerBase
    {
        private readonly IMaintenanceService _maintenanceService;

        public AdminSystemController(IMaintenanceService maintenanceService)
        {
            _maintenanceService = maintenanceService;
        }

        [HttpGet("maintenance")]
        public IActionResult GetMaintenanceStatus()
        {
            var config = _maintenanceService.GetConfig();
            return Ok(config);
        }

        [HttpPost("maintenance")]
        public IActionResult ToggleMaintenance([FromBody] MaintenanceConfigDto config)
        {
            _maintenanceService.UpdateConfig(config);
            return Ok(new { Message = "Maintenance configuration updated successfully", Config = config });
        }
    }
}
