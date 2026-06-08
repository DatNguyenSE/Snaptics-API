using System;
using System.Collections.Generic;
using System.Text;
using DAL.Enums;

namespace BLL.Dtos
{
    public class ItemInventoryDto
    {
            public int Id { get; set; }
            public string UserId { get; set; }
            public int TransactionDetailId { get; set; }

            public DateTime? ManufactureDate { get; set; }
            public DateTime? ExpiryDate { get; set; }
            public string? UsageFeedback { get; set; }
            
            public bool IsReviewed { get; set; }
            public DateTime? LastReviewDate { get; set; }
            public UsageStatusType UsageStatus { get; set; }
            public DateTime CreatedAt { get; set; }
    }
}
