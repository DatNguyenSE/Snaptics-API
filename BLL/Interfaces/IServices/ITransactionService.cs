using BLL.Dtos;

namespace BLL.Interfaces.IServices
{
    public interface ITransactionService
    {
        Task<IEnumerable<TransactionDto>>GetAllAsync();

        Task<TransactionDto> GetByIdAsync(int transactionId);

        Task<TransactionDto> CreateAsync(TransactionDto transactionDto);
        Task<TransactionDto> CreateWithDetailsAsync(CreateTransactionWithDetailsDto dto);
        Task<TransactionDto> CreateFromBillAsync(string userId, BLL.Dtos.AiDto.BillReadResultDto billDto);
        Task<TransactionDto> CreateFromImageAnalyzeAsync(string userId, BLL.Dtos.AiDto.AnalyzeImageResponseDto imageDto);

        Task<TransactionDto> UpdateAsync(int transactionId, TransactionDto transactionDto);

        Task<TransactionDto> DeleteAsync(int transactionId);

        Task<IEnumerable<TransactionDto>> GetByUserIdAsync(string userId);
    }
}