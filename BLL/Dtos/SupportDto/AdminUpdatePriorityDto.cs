using DAL.Enums;
using System;
using System.Collections.Generic;

namespace BLL.Dtos.Support
{
    public class AdminUpdatePriorityDto
    {
        public SupportTicketPriority Priority { get; set; }
    }
}
