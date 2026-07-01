using BLL.Dtos;
using BLL.Interfaces.IServices;
using Microsoft.AspNetCore.Mvc;
using DAL.Enums;

namespace API.Controllers
{
    [Route("[controller]")]
    public class ItemInventoryController(IItemInventoryService itemInventoryService, IItemReviewJobService itemReviewJobService) : BaseController<ItemInventoryController>
    {
        private string GetUserId()
        {
            return "user-12345-mock-id";
        }

        [HttpGet("user")]
        public async Task<ActionResult<IEnumerable<ItemInventoryDto>>> GetUserItemInventories()
        {
            try
            {
                var userId = GetUserId();
                var itemInventories = await itemInventoryService.GetByUserIdAsync(userId);
                return Ok(itemInventories);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Đã xảy ra lỗi khi lấy thông tin kho hàng cho người dùng.");
                return StatusCode(500, "Đã xảy ra lỗi khi lấy thông tin kho hàng.");
            }
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ItemInventoryDto>>> GetItemInventories()
        {
            try
            {
                var itemInventories = await itemInventoryService.GetAllAsync();
                return Ok(itemInventories);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Đã xảy ra lỗi khi lấy thông tin tất cả kho hàng.");
                return StatusCode(500, "Đã xảy ra lỗi khi lấy thông tin kho hàng.");
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ItemInventoryDto>> GetItemInventory(int id)
        {
            try
            {
                var itemInventory = await itemInventoryService.GetByIdAsync(id);
                if (itemInventory == null)
                {
                    return NotFound("Item Inventory not found");
                }
                return Ok(itemInventory);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"Đã xảy ra lỗi khi lấy thông tin kho hàng (ID: {id}).");
                return StatusCode(500, "Đã xảy ra lỗi khi lấy thông tin kho hàng.");
            }
        }

        [HttpPost]
        public async Task<ActionResult<ItemInventoryDto>> CreateItemInventory([FromBody] ItemInventoryDto itemInventoryDto)
        {
            try
            {
                var itemInventory = await itemInventoryService.CreateAsync(itemInventoryDto);
                Logger.LogInformation($"Tạo thành công kho hàng mới (ID: {itemInventory.Id})");
                return CreatedAtAction(nameof(GetItemInventory), new { id = itemInventory.Id }, itemInventory);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Lỗi Db: Tạo kho hàng mới thất bại.");
                return StatusCode(500, "Đã xảy ra lỗi khi tạo kho hàng mới.");
            }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<ItemInventoryDto>> UpdateItemInventory(int id, [FromBody] ItemInventoryDto itemInventoryDto)
        {
            if (id != itemInventoryDto.Id)
            {
                return BadRequest("ID trên đường dẫn không khớp với ID của kho hàng.");
            }
            try
            {
                var result = await itemInventoryService.UpdateAsync(id, itemInventoryDto);
                return Ok(result);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"Lỗi Db: Cập nhật kho hàng (ID: {id}) thất bại.");
                return StatusCode(500, "Đã xảy ra lỗi khi cập nhật kho hàng.");
            }
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult<ItemInventoryDto>> DeleteItemInventory(int id)
        {
            try
            {
                var result = await itemInventoryService.DeleteAsync(id);
                Logger.LogInformation($"Xóa thành công kho hàng (ID: {id})");
                return Ok(result);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"Lỗi Db: Xóa kho hàng (ID: {id}) thất bại.");
                return StatusCode(500, "Đã xảy ra lỗi khi xóa kho hàng.");
            }
        }

        [HttpGet("need-review")]
        public async Task<ActionResult<IEnumerable<ItemInventoryDto>>> GetItemsNeedReview([FromQuery] int days = 30)
        {
            try
            {
                var items = await itemInventoryService.GetItemsNeedReviewAsync(days);
                return Ok(items);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"Lỗi Db: Lấy thông tin kho hàng cần review (days={days}) thất bại.");
                return StatusCode(500, "Đã xảy ra lỗi khi lấy thông tin kho hàng cần review.");
            }
        }

        [HttpPut("{id}/review")]
        public async Task<ActionResult<ItemInventoryDto>> ReviewItem(int id, [FromQuery] UsageStatusType usageStatus)
        {
            try
            {
                var result = await itemInventoryService.ReviewItemAsync(id, usageStatus);
                Logger.LogInformation($"Đánh giá thành công kho hàng (ID: {id}) với trạng thái sử dụng: {usageStatus}");
                return Ok(result);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"Lỗi Db: Đánh giá kho hàng (ID: {id}) thất bại.");
                return StatusCode(500, "Đã xảy ra lỗi khi đánh giá kho hàng.");
            }
        }

        [HttpPost("test-trigger-item-review-scan")]
        public async Task<IActionResult> TestTriggerItemReviewScan([FromQuery] int days = 1)
        {
            try
            {
                Logger.LogInformation($"[JOB TRIGGER] Bắt đầu quét và gửi thông báo review item (days={days})...");
                await itemReviewJobService.ScanAndSendNotificationAsync(days);
                Logger.LogInformation($"[JOB TRIGGER] Quét và gửi thông báo review item (days={days}) hoàn tất.");
                return Ok(new
                {
                    Message = $"Item review scan completed for {days} day(s)."
                });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "[JOB THẤT BẠI] Quét và gửi thông báo review item bị sập giữa chừng!");
                return StatusCode(500, "Có lỗi xảy ra khi chạy tiến trình quét item.");
            }

        }
    }
}
