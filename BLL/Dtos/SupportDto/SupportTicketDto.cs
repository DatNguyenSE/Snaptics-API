using System;
using System.Collections.Generic;
using System.Text;
using DAL.Enums;

namespace BLL.Dtos.Support
{
    public class SupportTicketDto
    {
        public int Id { get; set; }
        public string? UserId { get; set; }
        public string? UserName { get; set; }
        public string? UserEmail { get; set; }
        public string? Subject { get; set; }
        public string? Description { get; set; }
        public SupportTicketStatus Status { get; set; }
        public SupportTicketPriority Priority { get; set; }
        public SupportTicketCategory Category { get; set; }
        public string? AssignedToId { get; set; }
        public string? AssignedToName { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? ResolvedAt { get; set; }
        public int MessageCount { get; set; }
    }

    public class SupportTicketDetailDto : SupportTicketDto
    {
        public List<SupportMessageDto> Messages { get; set; } = new();
        public List<SupportAttachmentDto> Attachments { get; set; } = new();
    }
}