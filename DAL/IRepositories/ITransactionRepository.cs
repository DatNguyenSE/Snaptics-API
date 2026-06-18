using System;
using System.Collections.Generic;
using System.Text;
using DAL.Entities;

namespace DAL.IRepositories
{
    public interface ITransactionRepository: IGenericRepository<Transaction>
    {
        Task<IEnumerable<DAL.Entities.Transaction>> GetByUserIdAsync(string userId);
    }
}
