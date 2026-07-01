using BLL.Dtos;
using BLL.Interfaces.IServices;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace API.Controllers
{
    [Route("[controller]")]
    public class NotificationController(INotificationService _notificationService) : BaseController<NotificationController>
    {
        private string GetUserId()
        {
            return "user-12345-mock-id";
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<NotificationDto>>> GetNotifications()
        {
            try
            {
                var notifications = await _notificationService.GetAllAsync();
                return Ok(notifications);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Lỗi Db: Lấy thông tin thông báo thất bại.");
                return StatusCode(500, "Đã xảy ra lỗi khi lấy thông tin thông báo.");
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<NotificationDto>> GetNotification(int id)
        {
            try
            {
                var notification = await _notificationService.GetByIdAsync(id);
                if (notification == null)
                {
                    return NotFound("Notification not found");
                }
                return Ok(notification);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"Lỗi Db: Lấy thông tin thông báo (ID: {id}) thất bại.");
                return StatusCode(500, "Đã xảy ra lỗi khi lấy thông tin thông báo.");
            }
        }

        [HttpPost]
        public async Task<ActionResult<NotificationDto>> CreateNotification([FromBody] NotificationDto notificationDto)
        {
            try
            {
                var notification = await _notificationService.CreateAsync(notificationDto);
                Logger.LogInformation($"Tạo thành công thông báo mới (ID: {notification.Id})");
                return CreatedAtAction(nameof(GetNotification), new { id = notification.Id }, notification);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Lỗi Db: Tạo thông báo mới thất bại.");
                return StatusCode(500, "Đã xảy ra lỗi khi tạo thông báo mới.");
            }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<NotificationDto>> UpdateNotification(int id, [FromBody] NotificationDto notificationDto)
        {
            if (id != notificationDto.Id)
            {
                return BadRequest("ID mismatch");
            }
            try
            {
                var result = await _notificationService.UpdateAsync(id, notificationDto);
                return Ok(result);
            }
            catch (System.Exception ex)
            {
                Logger.LogError(ex, $"Lỗi Db: Cập nhật thông báo (ID: {id}) thất bại.");
                return StatusCode(500, "Đã xảy ra lỗi khi cập nhật thông báo.");
            }
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult<NotificationDto>> DeleteNotification(int id)
        {
            try
            {
                var result = await _notificationService.DeleteAsync(id);
                Logger.LogInformation($"Xóa thành công thông báo (ID: {id})");
                return Ok(result);
            }
            catch (System.Exception ex)
            {
                Logger.LogError(ex, $"Lỗi Db: Xóa thông báo (ID: {id}) thất bại.");
                return StatusCode(500, "Đã xảy ra lỗi khi xóa thông báo.");
            }
        }

        [HttpGet("user")]
        public async Task<ActionResult<IEnumerable<NotificationDto>>> GetUserNotifications()
        {
            try
            {
                var userId = GetUserId();
                var notifications = await _notificationService.GetByUserIdAsync(userId);
                return Ok(notifications);
            }
            catch (System.Exception ex)
            {
                Logger.LogError(ex, $"LỖI DB: Lấy danh sách thông báo của user {GetUserId()} thất bại.");
                return StatusCode(500, "Không thể tải thông báo lúc này, vui lòng thử lại sau.");
            }
        }

        [HttpPost("test-trigger-cleanup")]
        public async Task<IActionResult> TestTriggerCleanup()
        {
            try
            {
                Logger.LogInformation("[JOB CLEANUP] Bắt đầu dọn dẹp các thông báo cũ trong hệ thống...");
                await _notificationService.CleanUpOldNotificationsAsync();
                Logger.LogInformation("[JOB CLEANUP] Dọn dẹp các thông báo cũ trong hệ thống hoàn tất.");
                return Ok();
            }
            catch (System.Exception ex)
            {
                Logger.LogError(ex, "[JOB THẤT BẠI] Dọn dẹp các thông báo cũ trong hệ thống bị sập giữa chừng!");
                return StatusCode(500, "Có lỗi xảy ra khi chạy tiến trình dọn dẹp thông báo.");
            }
        }
    }
}
