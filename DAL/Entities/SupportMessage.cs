using System;
using System.Collections.Generic;
using System.Text;

namespace DAL.Entities
{
    public class SupportMessage
    {
        public int Id { get; set; }
        public int TicketId { get; set; }
        public string? SenderId { get; set; }
        public string? Content { get; set; }
        public bool IsFromAdmin { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        public virtual SupportTicket Ticket { get; set; }
        public virtual AppUser Sender { get; set; }
        public virtual ICollection<SupportAttachment> Attachments { get; set; } = new List<SupportAttachment>();
    }
}