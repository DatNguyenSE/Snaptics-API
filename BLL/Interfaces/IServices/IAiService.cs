using BLL.Dtos.AiDto;
using Microsoft.AspNetCore.Http;

namespace BLL.Interfaces.IServices
{
    public interface IAiService
    {
        /// <summary>
        /// Gửi ảnh lên Google Gemini Vision kèm prompt chuyên gia dinh dưỡng.
        /// </summary>
        Task<AnalyzeImageResponseDto> AnalyzeImageAsync(byte[] imageBytes, string contentType, bool trackCalories = true, bool estimatePrice = true);

        /// <summary>
        /// Gửi ảnh hóa đơn/bill lên Azure Document Intelligence (prebuilt-receipt).
        /// </summary>
        Task<BillReadResultDto> ReadBillAsync(byte[] imageBytes, string contentType);
    }
}
