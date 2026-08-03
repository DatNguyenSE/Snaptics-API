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
            var s3Key = await _s3Service.UploadFileAsync(image, userId, "temp-ai");

            var aiTask = new AiTaskMessageDto
            {
                TaskType = "AnalyzeImage",
                S3ObjectKey = s3Key,
                ContentType = image.ContentType,
                UserId = userId,
                TrackCalories = trackCalories,
                EstimatePrice = estimatePrice
            };

            await _sqsPublisher.SendMessageAsync(aiTask);

            return Accepted(new { message = "Request accepted. Processing in background.", s3Key });
        }

        [HttpPost("analyze-image-sync")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> AnalyzeImageSync(
            IFormFile image,
            [FromQuery] bool estimatePrice = true)
        {
            if (image == null || image.Length == 0) return BadRequest("Vui lòng chọn file ảnh.");
            if (image.Length > 10 * 1024 * 1024) return BadRequest("Kích thước ảnh không được vượt quá 10MB.");

            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "unknown";

            byte[] imageBytes;
            using (var ms = new MemoryStream())
            {
                await image.CopyToAsync(ms);
                imageBytes = ms.ToArray();
            }

            try
            {
                var result = await _aiService.AnalyzeImageAsync(imageBytes, image.ContentType, userId, estimatePrice);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi gọi AI: " + ex.ToString() });
            }
        }

        [HttpGet("list-models")]
        public async Task<IActionResult> ListModels([FromServices] IConfiguration config, [FromServices] IHttpClientFactory httpClientFactory)
        {
            var apiKey = config["AiSettings:GeminiApiKey"];
            var url = $"https://generativelanguage.googleapis.com/v1beta/models?key={apiKey}";
            var client = httpClientFactory.CreateClient();
            var response = await client.GetAsync(url);
            var content = await response.Content.ReadAsStringAsync();
            return Ok(content);
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
            var s3Key = await _s3Service.UploadFileAsync(billImage, userId, "temp-ai");

            var aiTask = new AiTaskMessageDto
            {
                TaskType = "ReadBill",
                S3ObjectKey = s3Key,
                ContentType = billImage.ContentType,
                UserId = userId
            };

            await _sqsPublisher.SendMessageAsync(aiTask);

            return Accepted(new { message = "Request accepted. Processing in background.", s3Key });
        }
    }
}
