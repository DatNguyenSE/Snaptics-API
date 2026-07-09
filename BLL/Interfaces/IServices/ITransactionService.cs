using BLL.Dtos;
using BLL.Dtos.AiDto;

namespace BLL.Interfaces.IServices
{
    public interface ITransactionService
    {
        Task<IEnumerable<TransactionDto>>GetAllAsync();

        Task<TransactionDto> GetByIdAsync(int transactionId);

        Task<TransactionDto> CreateAsync(TransactionDto transactionDto);
        Task<TransactionDto> CreateWithDetailsAsync(CreateTransactionWithDetailsDto dto);
        Task<TransactionDto> CreateFromBillAsync(string userId, BillReadResultDto billDto, string billImageKey);
        Task<TransactionDto> CreateFromImageAnalyzeAsync(string userId, AnalyzeImageResponseDto imageDto, string ImageKey);

        Task<TransactionDto> UpdateAsync(int transactionId, TransactionDto transactionDto);

        Task<TransactionDto> DeleteAsync(int transactionId);
        Task<IEnumerable<TransactionDto>> GetUnconfirmedTransactionsByDateAsync(DateTime date);

        Task<IEnumerable<TransactionDto>> GetByUserIdAsync(string userId);
    }
}