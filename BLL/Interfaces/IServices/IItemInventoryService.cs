using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Text;
using System.Threading.Tasks;
using BLL.Dtos;
using DAL.Entities;

namespace BLL.Interfaces.IServices
{
    public interface IItemInventoryService
    {
        Task<IEnumerable<ItemInventoryDto>> GetAllAsync();
        Task<IEnumerable<ItemInventoryDto>> GetByUserIdAsync(string userId);
        Task<ItemInventoryDto> GetByIdAsync(int itemInventoryId);
        Task<ItemInventoryDto> CreateAsync(ItemInventoryDto itemInventoryDto);
        Task<ItemInventoryDto> UpdateAsync(int itemInventoryId, ItemInventoryDto itemInventoryDto);
        Task<ItemInventoryDto> DeleteAsync(int itemInventoryId);
    }
}
