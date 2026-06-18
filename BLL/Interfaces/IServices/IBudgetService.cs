using BLL.Dtos;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BLL.Interfaces.IServices
{
    public interface IBudgetService
    {
        Task<IEnumerable<BudgetDto>> GetAllAsync();
        Task<BudgetDto> GetByIdAsync(int id);
        Task<BudgetDto> CreateAsync(BudgetDto budgetDto);
        Task<BudgetDto> UpdateAsync(int id, BudgetDto budgetDto);
        Task<BudgetDto> DeleteAsync(int id);

        Task<IEnumerable<BudgetDto>> GetAwsBudgetsAsync();
    }
}
