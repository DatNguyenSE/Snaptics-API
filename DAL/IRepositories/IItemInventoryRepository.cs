using DAL.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace DAL.IRepositories
{
    public interface IItemInventoryRepository : IGenericRepository<ItemInventory>
    {
        Task<IEnumerable<ItemInventory>> GetByUserIdAsync(string userId);
        Task<IEnumerable<ItemInventory>> GetItemsNeedReviewWithDetailAsync(DateTime thresholdDate);
        Task<IEnumerable<ItemInventory>> GetNeedReviewItemsAsync(string userId);
    }
}
