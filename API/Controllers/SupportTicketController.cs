using API.Extensions;
using BLL.Dtos;
using BLL.Dtos.Support;
using BLL.Interfaces.IServices;
using DAL.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Authorize]
    [Route("api/support")]
    [ApiController]
    public class SupportTicketController(
        ISupportTicketService _supportTicketService
    ) : ControllerBase
    {
        /// <summary>
        /// Tạo yêu cầu hỗ trợ mới.
        /// </summary>
        [HttpPost("tickets")]
        public async Task<ActionResult<SupportTicketDto>> CreateTicket([FromBody] CreateSupportTicketDto dto)
        {
            var userId = User.GetUserId();
            var ticket = await _supportTicketService.CreateTicketAsync(userId, dto);
            return CreatedAtAction(nameof(GetTicketDetail), new { id = ticket.Id }, ticket);
        }

        /// <summary>
        /// Lấy danh sách ticket của user hiện tại.
        /// </summary>
        [HttpGet("tickets")]
        public async Task<ActionResult<PaginatedResultDto<SupportTicketDto>>> GetTickets(
            [FromQuery] string? search,
            [FromQuery] SupportTicketStatus? status,
            [FromQuery] SupportTicketCategory? category,
            [FromQuery] int page = 1,
            [FromQuery] int size = 10)
        {
            var userId = User.GetUserId();
            var query = new UserTicketQueryDto
            {
                Search = search,
                Status = status,
                Category = category,
                Page = page,
                Size = size
            };
            var result = await _supportTicketService.GetUserTicketsAsync(userId, query);
            return Ok(result);
        }

        /// <summary>
        /// Xem chi tiết ticket và lịch sử trao đổi.
        /// </summary>
        [HttpGet("tickets/{id}")]
        public async Task<ActionResult<SupportTicketDetailDto>> GetTicketDetail(int id)
        {
            var userId = User.GetUserId();
            var ticket = await _supportTicketService.GetUserTicketDetailAsync(userId, id);
            if (ticket == null) return NotFound("Ticket not found");
            return Ok(ticket);
        }

        /// <summary>
        /// User gửi phản hồi trong ticket.
        /// </summary>
        [HttpPost("tickets/{id}/messages")]
        public async Task<ActionResult<SupportMessageDto>> SendMessage(int id, [FromBody] SendMessageDto dto)
        {
            var userId = User.GetUserId();
            var message = await _supportTicketService.SendMessageAsync(userId, id, dto);
            return Ok(message);
        }

        /// <summary>
        /// Đóng ticket.
        /// </summary>
        [HttpPatch("tickets/{id}/close")]
        public async Task<ActionResult<SupportTicketDto>> CloseTicket(int id)
        {
            var userId = User.GetUserId();
            var ticket = await _supportTicketService.CloseTicketAsync(userId, id);
            return Ok(ticket);
        }

        /// <summary>
        /// Mở lại ticket đã đóng.
        /// </summary>
        [HttpPatch("tickets/{id}/reopen")]
        public async Task<ActionResult<SupportTicketDto>> ReopenTicket(int id)
        {
            var userId = User.GetUserId();
            var ticket = await _supportTicketService.ReopenTicketAsync(userId, id);
            return Ok(ticket);
        }

        /// <summary>
        /// Thống kê ticket của user.
        /// </summary>
        [HttpGet("tickets/statistics")]
        public async Task<ActionResult<SupportTicketStatisticsDto>> GetStatistics()
        {
            var userId = User.GetUserId();
            var stats = await _supportTicketService.GetUserStatisticsAsync(userId);
            return Ok(stats);
        }

        /// <summary>
        /// Upload file đính kèm.
        /// </summary>
        [HttpPost("attachments")]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<SupportAttachmentDto>> UploadAttachment(
        IFormFile file,
        [FromQuery] int? ticketId,
        [FromQuery] int? messageId)
        {
            var userId = User.GetUserId();
            var attachment = await _supportTicketService.UploadAttachmentAsync(userId, ticketId, messageId, file);
            return Ok(attachment);
        }
    }
}