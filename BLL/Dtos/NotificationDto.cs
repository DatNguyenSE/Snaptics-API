using System;
using DAL.Enums;

namespace BLL.Dtos
{
    public class NotificationDto
    {
        public int Id { get; set; }
        public required string UserId { get; set; }
        public required string Message { get; set; }
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
        public int? ItemInventoryId { get; set; }
        public NotificationType Type { get; set; }
        public int? TransactionDetailId { get; set; }
    }
}
