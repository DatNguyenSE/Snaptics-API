using BLL.Dtos;

namespace BLL.Interfaces.IServices
{
    public interface ITransactionService
    {
        Task<IEnumerable<TransactionDto>>GetAllAsync();

        Task<TransactionDto> GetByIdAsync(int transactionId);

        Task<TransactionDto>CreateAsync(TransactionDto transactionDto);

        Task<TransactionDto> UpdateAsync(int transactionId, TransactionDto transactionDto);

        Task<TransactionDto> DeleteAsync(int transactionId);
    }
}