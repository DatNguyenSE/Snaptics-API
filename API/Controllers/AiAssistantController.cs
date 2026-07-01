using API.Extensions;
using BLL.Dtos.AiAssistantDto;
using BLL.Interfaces.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Authorize]
    [Route("[controller]")]
    public class AiAssistantController(
        IAiAssistantService _aiAssistantService) : BaseController<AiAssistantController>
    {
        [HttpPost("ask")]
        public async Task<ActionResult<AskAiResponseDto>> Ask([FromBody] AskAiRequestDto request)
        {
            var userId = User.GetUserId();
            try
            {
                Logger.LogInformation($"User {userId} đang gửi câu hỏi cho AI Assistant.");
                var result = await _aiAssistantService.AskAsync(userId, request);

                return Ok(result);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"Failed to get AI response: {ex.Message}");
                return StatusCode(500, "An error occurred while processing your request.");
            }


        }
    }
}