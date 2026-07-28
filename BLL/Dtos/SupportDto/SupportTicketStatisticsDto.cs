using System;
using System.Collections.Generic;
using System.Text;

namespace BLL.Dtos.Support
{
    public class SupportTicketStatisticsDto
    {
        public int Total { get; set; }
        public int Pending { get; set; }
        public int InProgress { get; set; }
        public int WaitingForUser { get; set; }
        public int Resolved { get; set; }
        public int Closed { get; set; }
    }
}
