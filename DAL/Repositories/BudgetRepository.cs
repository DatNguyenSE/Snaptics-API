using DAL.Data;
using DAL.Entities;
using DAL.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace DAL.Repositories
{
    public class BudgetRepository(AppDbContext context) : GenericRepository<Budget>(context), IBudgetRepository
    {
        public async Task<IEnumerable<Budget>> GetByUserIdAsync(string userId)
        {
            return await _dbSet.Where(x => x.UserId == userId).ToListAsync();
        }
    }
}
