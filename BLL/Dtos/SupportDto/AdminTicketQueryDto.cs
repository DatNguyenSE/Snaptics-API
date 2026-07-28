using DAL.Enums;
using System;
using System.Collections.Generic;

namespace BLL.Dtos.Support
{
    public class AdminTicketQueryDto
    {
        public string? Search { get; set; }
        public SupportTicketStatus? Status { get; set; }
        public SupportTicketPriority? Priority { get; set; }
        public SupportTicketCategory? Category { get; set; }
        public string? AssignedToId { get; set; }
        public int Page { get; set; } = 1;
        public int Size { get; set; } = 10;
    }
}
