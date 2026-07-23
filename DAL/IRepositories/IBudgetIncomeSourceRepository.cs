using System;
using System.Collections.Generic;
using System.Text;
using DAL.Entities;

namespace DAL.IRepositories
{
    public interface IBudgetIncomeSourceRepository : IGenericRepository<BudgetIncomeSource>
    {
        Task<IEnumerable<BudgetIncomeSource>> GetByBudgetIdAsync(int budgetId);
        Task<BudgetIncomeSource?> GetByIdAsync(int id);
    }
}
