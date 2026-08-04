using API.Extensions;
using BLL.Dtos.AiAssistantDto;
using BLL.Interfaces.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("[controller]")]
    public class AiAssistantController(
        IAiAssistantService _aiAssistantService) : ControllerBase
    {
        [HttpPost("ask")]
        public async Task<ActionResult<AskAiResponseDto>> Ask([FromBody] AskAiRequestDto request)
        {
            var userId = User.GetUserId();

            var result = await _aiAssistantService.AskAsync(userId, request);

            return Ok(result);
        }

        [HttpPost("insight")]
        public async Task<IActionResult> TestInsight([FromServices] IAiInsightService aiInsightService)
        {
            var userId = User.GetUserId();

            var count = await aiInsightService.GenerateInsightsAsync(userId);

            return Ok(new
            {
                Message = "AI insight generated successfully.",
                NotificationsCreated = count
            });
        }

        [HttpGet("inventory-insight-export")]
        public async Task<IActionResult> ExportInventoryInsight([FromServices] IAiInsightService aiInsightService)
        {
            var bytes = await aiInsightService.ExportInventoryInsightCsvAsync();
            return File(bytes, "text/csv", "BaoCaoDoDac.csv");
        }
    }
}