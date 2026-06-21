using BLL.Dtos.AiDto;
using BLL.Interfaces.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    /// <summary>
    /// - Phân tích ảnh bằng AI (ChatGPT / gpt-4o-mini)
    /// - Đọc hóa đơn/bill bằng Azure Document Intelligence
    /// </summary>
    // [Authorize]
    [Route("ai")]
    [ApiController]
    public class AiController(IAiService _aiService, ICategoryService _CateService, IS3Service _s3Service) : ControllerBase
    {
        /// <summary>
        /// Tính năng 1: Phân tích ảnh bằng AI.
        /// Luồng hoạt động:
        /// 1. Nhận file ảnh từ người dùng.
        /// 2. Validate kích thước file.
        /// 3. Gửi ảnh sang AiService để AI (LLM) phân tích.
        /// 4. AI trả về tên item, loại, calo ước tính, giá VND ước tính.
        /// Client dùng kết quả này để gọi POST /TransactionDetail nếu muốn lưu.
        /// </summary>
        /// <param name="image">File ảnh (jpg, png, webp, heic)</param>
        [HttpPost("analyze-image")]
        [ProducesResponseType(typeof(AnalyzeImageResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<AnalyzeImageResponseDto>> AnalyzeImage(
            IFormFile image,
            [FromQuery] bool trackCalories = true,
            [FromQuery] bool estimatePrice = true)
        {
            // Bước 1: Validate đầu vào, đảm bảo có file và không trống
            if (image == null || image.Length == 0)
                return BadRequest("Vui lòng chọn file ảnh.");

            // Bước 2: Giới hạn dung lượng ảnh tối đa là 10MB để tránh overload
            if (image.Length > 10 * 1024 * 1024)
                return BadRequest("Kích thước ảnh không được vượt quá 10MB.");

            // Bước 3: Chuyển tiếp ảnh cho AiService để xử lý và phân tích
            var result = await _aiService.AnalyzeImageAsync(image, trackCalories, estimatePrice);

            // Bước 4: Upload ảnh lên S3 được chuyển sang TransactionController để tránh rác S3
            
            // Gắn key ảnh
            result.ImageKey = null;

            return Ok(result);
        }

        /// <summary>
        /// Tính năng 2: Đọc hóa đơn/bill bằng Azure Document Intelligence.
        /// Luồng hoạt động:
        /// 1. Nhận ảnh/PDF hóa đơn từ client.
        /// 2. Gửi lên Azure Document Intelligence để trích xuất dữ liệu (tên cửa hàng, tổng tiền, ngày mua...).
        /// 3. Lấy danh sách các món hàng (Items) và dùng LLM để tự động phân loại (Food/Object).
        /// Client confirm rồi gọi POST /Transaction + POST /TransactionDetail để lưu.
        /// </summary>
        /// <param name="billImage">File ảnh bill/hóa đơn (jpg, png, tiff, pdf)</param>
        [HttpPost("read-bill")]
        [ProducesResponseType(typeof(BillReadResultDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<BillReadResultDto>> ReadBill(IFormFile billImage)
        {
            // Bước 1: Kiểm tra xem client có gửi file lên không
            if (billImage == null || billImage.Length == 0)
                return BadRequest("Vui lòng chọn file hóa đơn.");

            // Bước 2: Giới hạn dung lượng file là 20MB (PDF có thể có dung lượng lớn hơn ảnh thường)
            if (billImage.Length > 20 * 1024 * 1024)
                return BadRequest("Kích thước file không được vượt quá 20MB.");

            // Bước 3: Gửi file qua AiService để dùng Azure nhận diện và bóc tách thông tin
            var result = await _aiService.ReadBillAsync(billImage);

            // Bước 4: Upload file lên S3 được chuyển sang TransactionController để tránh rác S3
            
            // Gắn key vào kết quả
            result.BillImageKey = null;

            return Ok(result);
        }
    }
}
