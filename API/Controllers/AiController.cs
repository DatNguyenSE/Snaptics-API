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
        [ProducesResponseType(StatusCodes.Status200OK)]
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
            
            var s3Key = await _s3Service.UploadFileAsync(image, userId, "ai-tasks");

            using var memoryStream = new MemoryStream();
            await image.CopyToAsync(memoryStream);
            var imageBytes = memoryStream.ToArray();

            var response = await _aiService.AnalyzeImageAsync(imageBytes, image.ContentType, userId, estimatePrice);

            return Ok(new {
                ItemName = response.ItemName,
                Category = response.Category,
                Quantity = response.Quantity,
                EstimatedCalories = response.EstimatedCalories,
                EstimatedPriceVND = response.EstimatedPriceVND,
                BudgetId = response.BudgetId,
                Unit = response.Unit,
                ImageKey = s3Key
            });
        }

        [HttpPost("read-bill")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> ReadBill(IFormFile billImage)
        {
            if (billImage == null || billImage.Length == 0) return BadRequest("Vui lòng chọn file hóa đơn.");
            if (billImage.Length > 20 * 1024 * 1024) return BadRequest("Kích thước file không được vượt quá 20MB.");

            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "unknown";

            var s3Key = await _s3Service.UploadFileAsync(billImage, userId, "ai-tasks");

            using var memoryStream = new MemoryStream();
            await billImage.CopyToAsync(memoryStream);
            var imageBytes = memoryStream.ToArray();

            var response = await _aiService.ReadBillAsync(imageBytes, billImage.ContentType);

            return Ok(new {
                MerchantName = response.MerchantName,
                TransactionDate = response.TransactionDate,
                TotalAmount = response.TotalAmount,
                Currency = response.Currency,
                BudgetId = response.BudgetId,
                Items = response.Items,
                BillImageKey = s3Key
            });
        }
    }
}
