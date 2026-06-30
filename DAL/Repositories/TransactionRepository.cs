using DAL.Data;
using System;
using System.Collections.Generic;
using System.Text;
using DAL.Entities;
using DAL.IRepositories;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore; 
using System.Linq;

namespace DAL.Repositories
{
    public class TransactionRepository(AppDbContext _context) : GenericRepository<Transaction>(_context), ITransactionRepository
    {
        public override async Task<IEnumerable<Transaction>> GetAllAsync()
        {
            return await _dbSet.Include(t => t.TransactionDetails).AsNoTracking().ToListAsync();
        }

        public override async Task<Transaction?> GetByIdAsync(int id)
        {
            return await _dbSet.Include(t => t.TransactionDetails).FirstOrDefaultAsync(t => t.Id == id);
        
        }
        public async Task<IEnumerable<DAL.Entities.Transaction>> GetByUserIdAsync(string userId)
        {
            return await _dbSet.Where(x => x.UserId == userId).ToListAsync();
        }
        public async Task<IEnumerable<Transaction>> GetCompletedTransactionsWithDetailsAsync(string userId, DateTime from, DateTime to)
        {
            return await _dbSet
                .Include(t => t.TransactionDetails)
                    .ThenInclude(td => td.Category)
                .Where(t =>
                    t.UserId == userId &&
                    !t.IsDeleted &&
                    t.Status == DAL.Enums.TransactionStatusType.Completed &&
                    t.TransactionDate >= from &&
                    t.TransactionDate < to)
                .ToListAsync();
        }
    }
}