using DAL.Entities;
using System.Threading.Tasks;

namespace DAL.IRepositories
{
    public interface IBudgetMemberRepository : IGenericRepository<BudgetMember>
    {
        Task<BudgetMember> GetByBudgetAndMemberAsync(int budgetId, string memberId);
        Task<IEnumerable<BudgetMember>> GetMembersByBudgetIdAsync(int budgetId);
        Task<IEnumerable<BudgetMember>> GetSharedBudgetsByUserIdAsync(string userId);
    }
}
