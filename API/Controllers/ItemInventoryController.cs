using BLL.Dtos;
using BLL.Interfaces.IServices;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Route("[controller]")]
    public class ItemInventoryController(IItemInventoryService itemInventoryService) : Controller
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
                return BadRequest(ex.Message);
            }
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ItemInventoryDto>>> GetItemInventories()
        {
            var itemInventories = await itemInventoryService.GetAllAsync();
            return Ok(itemInventories);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ItemInventoryDto>> GetItemInventory(int id)
        {
            var itemInventory = await itemInventoryService.GetByIdAsync(id);
            if (itemInventory == null)
            {
                return NotFound("Item Inventory not found");
            }
            return Ok(itemInventory);
        }

        [HttpPost]
        public async Task<ActionResult<ItemInventoryDto>> CreateItemInventory([FromBody] ItemInventoryDto itemInventoryDto)
        {
            var itemInventory = await itemInventoryService.CreateAsync(itemInventoryDto);
            return CreatedAtAction(nameof(GetItemInventory), new { id = itemInventory.Id }, itemInventory);
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
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult<ItemInventoryDto>> DeleteItemInventory(int id)
        {
            try
            {
                var result = await itemInventoryService.DeleteAsync(id);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
