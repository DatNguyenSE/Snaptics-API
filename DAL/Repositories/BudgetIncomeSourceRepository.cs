using System;
using System.Collections.Generic;
using System.Text;
using DAL.Data;
using DAL.Entities;
using DAL.IRepositories;

namespace DAL.Repositories
{
    public class BudgetIncomeSourceRepository(AppDbContext context)
        : GenericRepository<BudgetIncomeSource>(context),
          IBudgetIncomeSourceRepository
    {
    }
}
