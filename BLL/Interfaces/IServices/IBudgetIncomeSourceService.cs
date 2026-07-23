using BLL.Dtos;
using System;
using System.Collections.Generic;
using System.Text;

namespace BLL.Interfaces.IServices
{
    public interface IBudgetIncomeSourceService
    {
        Task<IEnumerable<BudgetIncomeSourceDto>> GetByBudgetIdAsync(int budgetId);

        Task<BudgetIncomeSourceDto> AddAsync(BudgetIncomeSourceDto dto);

        Task<BudgetIncomeSourceDto> UpdateAsync(int id, BudgetIncomeSourceDto dto);

        Task DeleteAsync(int id);
    }
}
