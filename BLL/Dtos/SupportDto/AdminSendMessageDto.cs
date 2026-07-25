using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;

namespace BLL.Dtos.Support
{
    public class AdminSendMessageDto
    {
        public string Content { get; set; } = null!;
        public IFormFile? Attachment { get; set; }
    }
}
