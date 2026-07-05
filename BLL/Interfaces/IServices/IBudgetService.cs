using BLL.Dtos;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BLL.Interfaces.IServices
{
    public interface IBudgetService
    {
        Task<IEnumerable<BudgetDto>> GetAllAsync();

        Task<IEnumerable<BudgetDto>> GetByUserIdAsync(string userId);
        Task<BudgetDto> GetByIdAsync(int id);
        Task<BudgetDto> CreateAsync(BudgetDto budgetDto);
        Task<BudgetDto> UpdateAsync(int id, BudgetDto budgetDto);
        Task<BudgetDto> DeleteAsync(int id);

        Task<bool> DeductMoneyAsync(string userId, decimal amount, string note, int? budgetId = null, bool isAiEstimated = false);
        Task<IEnumerable<TransactionDto>> GetBudgetHistoryAsync(string userId, int budgetId);
    }
}
