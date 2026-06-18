using DAL.Data;
using System;
using System.Collections.Generic;
using System.Text;
using DAL.Entities;
using DAL.IRepositories;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

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
    }
}
