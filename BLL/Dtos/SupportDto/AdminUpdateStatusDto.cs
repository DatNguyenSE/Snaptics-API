using DAL.Enums;
using System;
using System.Collections.Generic;

namespace BLL.Dtos.Support
{
    public class AdminUpdateStatusDto
    {
        public SupportTicketStatus Status { get; set; }
    }
}
