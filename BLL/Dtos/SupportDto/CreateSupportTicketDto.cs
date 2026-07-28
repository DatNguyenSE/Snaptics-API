using System;
using System.Collections.Generic;
using System.Text;

using DAL.Enums;

namespace BLL.Dtos.Support
{
    public class CreateSupportTicketDto
    {
        public required string Subject { get; set; }
        public required string Description { get; set; }
        public SupportTicketCategory Category { get; set; } = SupportTicketCategory.General;
    }
}
