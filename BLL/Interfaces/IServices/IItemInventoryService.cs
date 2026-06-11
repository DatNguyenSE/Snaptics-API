using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Text;
using System.Threading.Tasks;
using BLL.Dtos;

namespace BLL.Interfaces.IServices
{
    public interface IItemInventoryService
    {
        Task<IEnumerable<ItemInventoryDto>> GetAllAsync();
        Task<ItemInventoryDto> GetByIdAsync(int itemInventoryId);
        Task<ItemInventoryDto> CreateAsync(ItemInventoryDto itemInventoryDto);
        Task<ItemInventoryDto> UpdateAsync(int itemInventoryId, ItemInventoryDto itemInventoryDto);
        Task<ItemInventoryDto> DeleteAsync(int itemInventoryId);
        Task<IEnumerable<ItemInventoryDto>> GetItemsNeedReviewAsync();
    }
}
