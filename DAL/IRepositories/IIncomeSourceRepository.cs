using System;
using System.Collections.Generic;
using System.Text;
using DAL.Entities;
        
namespace DAL.IRepositories
{
    public interface IIncomeSourceRepository : IGenericRepository<IncomeSource>
    {
        Task<IEnumerable<IncomeSource>> GetByUserIdAsync(string userId);
    }
}
