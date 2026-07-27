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
    [Authorize]
    [Route("ai")]
    [ApiController]
    public class AiController(IAiService _aiService, ICategoryService _CateService, IS3Service _s3Service, ISqsPublisherService _sqsPublisher) : ControllerBase
    {
        [HttpPost("analyze-image")]
        [ProducesResponseType(StatusCodes.Status202Accepted)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> AnalyzeImage(
            IFormFile image,
            [FromQuery] bool trackCalories = true,
            [FromQuery] bool estimatePrice = true)
        {
            if (image == null || image.Length == 0) return BadRequest("Vui lòng chọn file ảnh.");
            if (image.Length > 10 * 1024 * 1024) return BadRequest("Kích thước ảnh không được vượt quá 10MB.");

            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "unknown";
            
            // 1. Upload ảnh lên S3
            var s3Key = await _s3Service.UploadFileAsync(image, userId, "ai-tasks");

            // 2. Tạo tin nhắn SQS
            var aiTask = new AiTaskMessageDto
            {
                TaskType = "AnalyzeImage",
                S3ObjectKey = s3Key,
                ContentType = image.ContentType,
                UserId = userId,
                TrackCalories = trackCalories,
                EstimatePrice = estimatePrice
            };

            // 3. Đẩy vào hàng đợi
            await _sqsPublisher.SendMessageAsync(aiTask);

            // 4. Trả về ngay lập tức
            return Accepted(new { message = "Đang xử lý phân tích ảnh. Kết quả sẽ được gửi qua thông báo (SignalR)." });
        }

        [HttpPost("read-bill")]
        [ProducesResponseType(StatusCodes.Status202Accepted)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> ReadBill(IFormFile billImage)
        {
            if (billImage == null || billImage.Length == 0) return BadRequest("Vui lòng chọn file hóa đơn.");
            if (billImage.Length > 20 * 1024 * 1024) return BadRequest("Kích thước file không được vượt quá 20MB.");

            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "unknown";

            var s3Key = await _s3Service.UploadFileAsync(billImage, userId, "ai-tasks");

            var aiTask = new AiTaskMessageDto
            {
                TaskType = "ReadBill",
                S3ObjectKey = s3Key,
                ContentType = billImage.ContentType,
                UserId = userId
            };

            await _sqsPublisher.SendMessageAsync(aiTask);

            return Accepted(new { message = "Đang đọc hóa đơn. Kết quả sẽ được gửi qua thông báo (SignalR)." });
        }
    }
}
