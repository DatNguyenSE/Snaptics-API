using System;
using System.Collections.Generic;
using System.Text;
using DAL.Enums;

namespace BLL.Dtos.Support
{
    public class UserTicketQueryDto
    {
        public string? Search { get; set; }
        public SupportTicketStatus? Status { get; set; }
        public SupportTicketCategory? Category { get; set; }
        public int Page { get; set; } = 1;
        public int Size { get; set; } = 10;
    }
}
