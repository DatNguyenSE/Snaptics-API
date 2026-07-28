using System;
using System.Collections.Generic;
using System.Text;

namespace BLL.Dtos.Support
{
    public class SupportMessageDto
    {
        public int Id { get; set; }
        public int TicketId { get; set; }
        public string? SenderId { get; set; }
        public string? SenderName { get; set; }
        public string? Content { get; set; }
        public bool IsFromAdmin { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<SupportAttachmentDto> Attachments { get; set; } = new();
    }

    public class SendMessageDto
    {
        public required string Content { get; set; }
    }
}
