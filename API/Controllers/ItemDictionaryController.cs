using BLL.Dtos;
using BLL.Interfaces.IServices;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace API.Controllers
{
    [Route("[controller]")]
    public class ItemDictionaryController(IItemDictionaryService _itemDictionaryService) : BaseController<ItemDictionaryController>
    {
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ItemDictionaryDto>>> GetItemDictionaries()
        {
            try
            {
                var items = await _itemDictionaryService.GetAllAsync();
                return Ok(items);
            }
            catch (System.Exception ex)
            {
                Logger.LogError(ex, "LỖI DB: Lấy thông tin ItemDictionary thất bại.");
                return StatusCode(500, "Đã xảy ra lỗi khi lấy thông tin danh mục.");
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ItemDictionaryDto>> GetItemDictionary(int id)
        {
            try
            {
                var item = await _itemDictionaryService.GetByIdAsync(id);
                if (item == null)
                {
                    return NotFound("ItemDictionary not found");
                }
                return Ok(item);
            }
            catch (System.Exception ex)
            {
                Logger.LogError(ex, $"LỖI DB: Lấy thông tin ItemDictionary (ID: {id}) thất bại.");
                return StatusCode(500, "Đã xảy ra lỗi khi lấy thông tin danh mục.");
            }
        }

        [HttpPost]
        public async Task<ActionResult<ItemDictionaryDto>> CreateItemDictionary([FromBody] ItemDictionaryDto itemDictionaryDto)
        {
            try
            {
                var item = await _itemDictionaryService.CreateAsync(itemDictionaryDto);
                Logger.LogInformation($"Tạo thành công ItemDictionary mới (ID: {item.Id})");
                return CreatedAtAction(nameof(GetItemDictionary), new { id = item.Id }, item);
            }
            catch (System.Exception ex)
            {
                Logger.LogError(ex, "LỖI DB: Tạo ItemDictionary mới bằng DTO thất bại.");
                return StatusCode(500, "Không thể tạo danh mục lúc này, vui lòng thử lại sau.");
            }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<ItemDictionaryDto>> UpdateItemDictionary(int id, [FromBody] ItemDictionaryDto itemDictionaryDto)
        {
            if (id != itemDictionaryDto.Id)
            {
                return BadRequest("ID mismatch");
            }
            try
            {
                var result = await _itemDictionaryService.UpdateAsync(id, itemDictionaryDto);
                return Ok(result);
            }
            catch (System.Exception ex)
            {
                Logger.LogError(ex, $"LỖI DB: Cập nhật ItemDictionary (ID: {id}) thất bại.");
                return StatusCode(500, "Đã xảy ra lỗi khi cập nhật danh mục.");
            }
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult<ItemDictionaryDto>> DeleteItemDictionary(int id)
        {
            try
            {
                var result = await _itemDictionaryService.DeleteAsync(id);
                Logger.LogInformation($"Xóa thành công ItemDictionary (ID: {id})");
                return Ok(result);
            }
            catch (System.Exception ex)
            {
                Logger.LogError(ex, $"LỖI DB: Xóa ItemDictionary (ID: {id}) thất bại.");
                return StatusCode(500, "Đã xảy ra lỗi khi xóa danh mục.");
            }
        }

        [HttpPost("cleanup")]
        public async Task<ActionResult> CleanupDictionaries([FromQuery] int maxHitCount = 1, [FromQuery] int olderThanDays = 30)
        {
            try
            {
                Logger.LogInformation($"Bắt đầu dọn dẹp ItemDictionary (maxHitCount <= {maxHitCount}, olderThanDays = {olderThanDays})");
                var count = await _itemDictionaryService.CleanupAsync(maxHitCount, olderThanDays);
                Logger.LogInformation($"Dọn dẹp hoàn tất: Đã xóa thành công {count} bản ghi rác khỏi hệ thống.");
                return Ok(new { Message = $"Deleted {count} item dictionary entries with hit count <= {maxHitCount} and older than {olderThanDays} days." });
            }
            catch (System.Exception ex)
            {
                Logger.LogError(ex, $"LỖI DB: Dọn dẹp từ điển thất bại (maxHitCount: {maxHitCount}, days: {olderThanDays}).");
                return StatusCode(500, "Đã xảy ra lỗi hệ thống khi dọn dẹp dữ liệu, vui lòng thử lại sau.");
            }
        }
    }
}
