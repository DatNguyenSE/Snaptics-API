using System;
using System.Collections.Generic;
using System.Text;

namespace DAL.Entities
{
    public class SupportAttachment
    {
        public int Id { get; set; }
        public int? TicketId { get; set; }
        public int? MessageId { get; set; }
        public string? FileUrl { get; set; }
        public string? FileName { get; set; }
        public string? FileType { get; set; }
        public long FileSize { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        public virtual SupportTicket? Ticket { get; set; }
        public virtual SupportMessage? Message { get; set; }
    }
}