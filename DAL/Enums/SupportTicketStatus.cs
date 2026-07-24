using System;
using System.Collections.Generic;
using System.Text;

namespace DAL.Enums
{
    public enum SupportTicketStatus
    {
        Pending = 0,
        InProgress = 1,
        WaitingForUser = 2,
        Resolved = 3,
        Closed = 4
    }
}
