using DAL.Entities;
using System;
using System.Collections.Generic;
using System.Text;
using DAL.IRepositories;
using DAL.Data;
using Microsoft.EntityFrameworkCore;

namespace DAL.Repositories
{
    public class ItemInventoryRepository : GenericRepository<ItemInventory>, IItemInventoryRepository
    {
        public ItemInventoryRepository(AppDbContext context) : base(context)
        {

        }
        public async Task<IEnumerable<ItemInventory>> GetByUserIdAsync(string userId)
        {
            return await _dbSet.Where(x => x.UserId == userId).ToListAsync();
        }
    }
}
