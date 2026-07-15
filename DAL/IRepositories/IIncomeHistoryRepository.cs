using DAL.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace DAL.IRepositories
{
    public interface IIncomeHistoryRepository
        : IGenericRepository<IncomeHistory>
    {
        Task<bool> HasReceivedThisMonthAsync(int incomeSourceId);
    }
}
