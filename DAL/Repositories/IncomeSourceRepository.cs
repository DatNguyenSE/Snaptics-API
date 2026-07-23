using DAL.Data;
using DAL.Entities;
using DAL.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace DAL.Repositories
{
    public class IncomeSourceRepository(AppDbContext context): GenericRepository<IncomeSource>(context),
          IIncomeSourceRepository
    {
        public async Task<IEnumerable<IncomeSource>> GetByUserIdAsync(string userId)
        {
            return await _dbSet
                .Where(x => x.UserId == userId)
                .ToListAsync();
        }

        public override async Task<IncomeSource?> GetByIdAsync(int id)
        {
            return await _dbSet
                .FirstOrDefaultAsync(x => x.Id == id);
        }
    }
}