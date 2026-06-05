using System;
using System.Collections.Generic;
using System.Text;
using BLL.Dtos;

namespace BLL.Interfaces.IServices
{
    public interface ITransactionDetailService
    {
        Task<IEnumerable<TransactionDetailDto>> GetAllAsync();
        Task<TransactionDetailDto> GetByIdAsync(int transactionDetailId);
        Task<TransactionDetailDto> CreateAsync(TransactionDetailDto transactionDetailDto);
        Task<TransactionDetailDto> UpdateAsync(int transactionDetailId, TransactionDetailDto transactionDetailDto);

        Task<TransactionDetailDto> DeleteAsync(int transactionDetailId);
    }
}
