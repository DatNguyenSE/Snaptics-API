using BLL.Dtos.Support;
using BLL.Interfaces.IServices;
using DAL.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;

namespace API.Controllers
{
    [Route("api/admin/support/tickets")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AdminSupportTicketController : ControllerBase
    {
        private readonly ISupportTicketService _supportTicketService;

        public AdminSupportTicketController(ISupportTicketService supportTicketService)
        {
            _supportTicketService = supportTicketService;
        }

        [HttpGet]
        public async Task<IActionResult> GetTickets([FromQuery] AdminTicketQueryDto query)
        {
            var result = await _supportTicketService.AdminGetTicketsAsync(query);
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetTicketById(int id)
        {
            var result = await _supportTicketService.AdminGetTicketDetailAsync(id);
            if (result == null) return NotFound("Ticket not found.");
            return Ok(result);
        }

        [HttpPatch("{id}/assign")]
        public async Task<IActionResult> AssignTicket(int id, [FromBody] AdminAssignTicketDto request)
        {
            try
            {
                var result = await _supportTicketService.AdminAssignTicketAsync(id, request.AssignedToId);
                return Ok(result);
            }
            catch (System.Collections.Generic.KeyNotFoundException)
            {
                return NotFound("Ticket not found.");
            }
        }

        [HttpPatch("{id}/status")]
        public async Task<IActionResult> UpdateTicketStatus(int id, [FromBody] AdminUpdateStatusDto request)
        {
            try
            {
                var result = await _supportTicketService.AdminUpdateTicketStatusAsync(id, request.Status);
                return Ok(result);
            }
            catch (System.Collections.Generic.KeyNotFoundException)
            {
                return NotFound("Ticket not found.");
            }
        }

        [HttpPatch("{id}/priority")]
        public async Task<IActionResult> UpdateTicketPriority(int id, [FromBody] AdminUpdatePriorityDto request)
        {
            try
            {
                var result = await _supportTicketService.AdminUpdateTicketPriorityAsync(id, request.Priority);
                return Ok(result);
            }
            catch (System.Collections.Generic.KeyNotFoundException)
            {
                return NotFound("Ticket not found.");
            }
        }

        [HttpPost("{id}/messages")]
        public async Task<IActionResult> AddMessage(int id, [FromForm] AdminSendMessageDto request)
        {
            var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(adminId)) return Unauthorized();

            try
            {
                var result = await _supportTicketService.AdminSendMessageAsync(adminId, id, request);
                return Ok(result);
            }
            catch (System.Collections.Generic.KeyNotFoundException)
            {
                return NotFound("Ticket not found.");
            }
        }
    }
}
