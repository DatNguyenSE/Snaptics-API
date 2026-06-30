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
        IAiAssistantService _aiAssistantService) : Controller
    {
        [HttpPost("ask")]
        public async Task<ActionResult<AskAiResponseDto>> Ask([FromBody] AskAiRequestDto request)
        {
            var userId = User.GetUserId();

            var result = await _aiAssistantService.AskAsync(userId, request);

            return Ok(result);
        }
    }
}