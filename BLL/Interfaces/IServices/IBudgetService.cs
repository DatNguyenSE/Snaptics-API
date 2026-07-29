using BLL.Dtos;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BLL.Interfaces.IServices
{
    public interface IBudgetService
    {
        Task<IEnumerable<BudgetDto>> GetAllAsync();
        Task<IEnumerable<BudgetDto>> GetByUserIdAsync(string userId);
        Task<IEnumerable<BudgetDto>> GetAllAccessibleBudgetsAsync(string userId);
        Task<BudgetDto> GetByIdAsync(int id);
        Task<BudgetDto> CreateAsync(string userId, BudgetDto budgetDto);
        Task<BudgetDto> UpdateAsync(int id, BudgetDto budgetDto);
        Task<BudgetDto> DeleteAsync(int id);
        
        Task<BudgetDto> DepositAsync(string userId, int budgetId, DepositBudgetDto dto);

        Task<int> DeductMoneyAsync(string userId, decimal amount, int? budgetId = null, bool isExpense = true);
        Task<IEnumerable<TransactionDto>> GetBudgetHistoryAsync(string userId, int budgetId);
        Task<IEnumerable<IncomeHistoryDto>> GetIncomeHistoryAsync(string userId, int budgetId);
        Task ProcessPeriodicRolloverAsync();
    }
}
