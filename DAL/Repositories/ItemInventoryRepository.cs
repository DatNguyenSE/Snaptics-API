using DAL.Entities;
using System;
using System.Collections.Generic;
using System.Text;
using DAL.IRepositories;
using DAL.Data;

namespace DAL.Repositories
{
    public class ItemInventoryRepository : GenericRepository<ItemInventory>, IItemInventoryRepository
    {
        public ItemInventoryRepository(AppDbContext context) : base(context)
        {

        }
}
}
