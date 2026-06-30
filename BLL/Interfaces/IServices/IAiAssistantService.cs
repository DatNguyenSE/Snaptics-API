using BLL.Dtos.AiAssistantDto;
using System;
using System.Collections.Generic;
using System.Text;

namespace BLL.Interfaces.IServices
{
    public interface IAiAssistantService
    {
        Task<AskAiResponseDto> AskAsync(string userId, AskAiRequestDto request);
    }
}
