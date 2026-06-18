using DAL.Data;
using System;
using System.Collections.Generic;
using System.Text;
using DAL.Entities;
using DAL.IRepositories;
using Microsoft.EntityFrameworkCore; 
using System.Linq;

namespace DAL.Repositories
{
    public class TransactionRepository(AppDbContext _context) : GenericRepository<Transaction>(_context), ITransactionRepository
    {
        public async Task<IEnumerable<DAL.Entities.Transaction>> GetByUserIdAsync(string userId)
        {
            return await _dbSet.Where(x => x.UserId == userId).ToListAsync();
        }
    }
}
