using System;
using System.Collections.Generic;
using System.Text;

using DAL.Enums;

namespace DAL.Entities
{
    public class SupportTicket
    {
        public int Id { get; set; }
        public string? UserId { get; set; }
        public string? Subject { get; set; }
        public string? Description { get; set; }
        public SupportTicketStatus Status { get; set; } = SupportTicketStatus.Pending;
        public SupportTicketPriority Priority { get; set; } = SupportTicketPriority.Normal;
        public SupportTicketCategory Category { get; set; } = SupportTicketCategory.General;
        public string? AssignedToId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public DateTime? ResolvedAt { get; set; }

        // Navigation Properties
        public virtual AppUser User { get; set; }
        public virtual AppUser? AssignedTo { get; set; }
        public virtual ICollection<SupportMessage> Messages { get; set; } = new List<SupportMessage>();
        public virtual ICollection<SupportAttachment> Attachments { get; set; } = new List<SupportAttachment>();
    }
}
