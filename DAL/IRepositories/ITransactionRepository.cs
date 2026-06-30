using System;
using System.Collections.Generic;
using System.Text;
using DAL.Entities;

namespace DAL.IRepositories
{
    public interface ITransactionRepository: IGenericRepository<Transaction>
    {
        Task<IEnumerable<Transaction>> GetByUserIdAsync(string userId);

        Task<IEnumerable<Transaction>> GetCompletedTransactionsWithDetailsAsync(string userId, DateTime from, DateTime to);
    }
}
