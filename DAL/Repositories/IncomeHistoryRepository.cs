using DAL.Data;
using DAL.Entities;
using DAL.IRepositories;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;

namespace DAL.Repositories
{
    public class IncomeHistoryRepository(AppDbContext context): GenericRepository<IncomeHistory>(context),IIncomeHistoryRepository
    {
        public async Task<bool> HasReceivedThisMonthAsync(int incomeSourceId)
        {
            var now = DateTime.UtcNow;

            return await _dbSet.AnyAsync(x =>
                x.IncomeSourceId == incomeSourceId &&
                x.ReceivedDate.Year == now.Year &&
                x.ReceivedDate.Month == now.Month);
        }
    }
}
