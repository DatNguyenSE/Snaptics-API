using BLL.Dtos.AiDto;
using Microsoft.AspNetCore.Http;

namespace BLL.Interfaces.IServices
{
    public interface IAiService
    {
        /// <summary>
        /// Gửi ảnh lên Google Gemini Vision kèm prompt chuyên gia dinh dưỡng.
        /// Trả về JSON phân tích: tên món, loại, calo ước tính, giá VND ước tính.
        /// </summary>
        Task<AnalyzeImageResponseDto> AnalyzeImageAsync(IFormFile image, bool trackCalories = true, bool estimatePrice = true);

        /// <summary>
        /// Gửi ảnh hóa đơn/bill lên Azure Document Intelligence (prebuilt-receipt).
        /// Trả về danh sách items đã parse: tên, số lượng, giá.
        /// Client tự quyết định save vào TransactionDetail.
        /// </summary>
        Task<BillReadResultDto> ReadBillAsync(IFormFile billImage);
    }
}
