using System;
using System.Collections.Generic;
using System.Text;
using DAL.Data;
using DAL.Entities;
using DAL.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace DAL.Repositories
{
    public class BudgetIncomeSourceRepository(AppDbContext context): GenericRepository<BudgetIncomeSource>(context), IBudgetIncomeSourceRepository
    {
        public async Task<IEnumerable<BudgetIncomeSource>> GetByBudgetIdAsync(int budgetId)
        {
            return await _context.BudgetIncomeSources
                .Where(x => x.BudgetId == budgetId)
                .Include(x => x.IncomeSource)
                .ToListAsync();
        }
    }
}
