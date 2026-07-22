using DAL.Data;
using DAL.Entities;
using DAL.IRepositories;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace DAL.Repositories
{
    public class BudgetMemberRepository : GenericRepository<BudgetMember>, IBudgetMemberRepository
    {
        private readonly AppDbContext _context;

        public BudgetMemberRepository(AppDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<BudgetMember> GetByBudgetAndMemberAsync(int budgetId, string memberId)
        {
            return await _context.BudgetMembers
                .FirstOrDefaultAsync(bm => bm.BudgetId == budgetId && bm.MemberId == memberId);
        }

        public async Task<IEnumerable<BudgetMember>> GetMembersByBudgetIdAsync(int budgetId)
        {
            return await _context.BudgetMembers
                .Include(bm => bm.Member)
                .Where(bm => bm.BudgetId == budgetId)
                .ToListAsync();
        }

        public async Task<IEnumerable<BudgetMember>> GetSharedBudgetsByUserIdAsync(string userId)
        {
            return await _context.BudgetMembers
                .Include(bm => bm.Budget)
                .Where(bm => bm.MemberId == userId)
                .ToListAsync();
        }
    }
}
