using DAL.Data;
using DAL.Entities;
using DAL.IRepositories;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DAL.Repositories
{
    public class TransactionDetailRepository : GenericRepository<TransactionDetail>, ITransactionDetailRepository
    {
        public TransactionDetailRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<TransactionDetail>> GetDetailsWithoutInventoryAsync(DateTime thresholdDate)
        {
            return await _dbSet
                .Include(td => td.Transaction)
                .Where(td => 
                    td.Transaction.TransactionDate <= thresholdDate && 
                    !td.Transaction.IsDeleted &&
                    td.ItemInventory == null)
                .ToListAsync();
        }
    }
}
