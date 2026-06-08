using BLL.Dtos;
using BLL.Interfaces.IServices;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace API.Controllers
{
    [Route("[controller]")]
    public class ItemDictionaryController(IItemDictionaryService _itemDictionaryService) : Controller
    {
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ItemDictionaryDto>>> GetItemDictionaries()
        {
            var items = await _itemDictionaryService.GetAllAsync();
            return Ok(items);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ItemDictionaryDto>> GetItemDictionary(int id)
        {
            var item = await _itemDictionaryService.GetByIdAsync(id);
            if (item == null)
            {
                return NotFound("ItemDictionary not found");
            }
            return Ok(item);
        }

        [HttpPost]
        public async Task<ActionResult<ItemDictionaryDto>> CreateItemDictionary([FromBody] ItemDictionaryDto itemDictionaryDto)
        {
            var item = await _itemDictionaryService.CreateAsync(itemDictionaryDto);
            return CreatedAtAction(nameof(GetItemDictionary), new { id = item.Id }, item);
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
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult<ItemDictionaryDto>> DeleteItemDictionary(int id)
        {
            try
            {
                var result = await _itemDictionaryService.DeleteAsync(id);
                return Ok(result);
            }
            catch (System.Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
