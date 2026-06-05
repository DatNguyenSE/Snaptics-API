using System;
using System.Collections.Generic;
using System.Text;

namespace BLL.Dtos
{
    public class ItemInventoryDto
    {
            public int Id { get; set; }
            public string UserId { get; set; }
            public int TransactionDetailId { get; set; }

            public int UsageStatus { get; set; }
            public DateTime? LastCheckedDate { get; set; }
    }
}
