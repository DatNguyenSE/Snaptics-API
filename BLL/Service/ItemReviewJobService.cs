using BLL.Interfaces.IServices;
using DAL.Entities;
using DAL.IRepositories;
using DAL.Enums;

namespace BLL.Service
{
    public class ItemReviewJobService(IUnitOfWork _uow) : IItemReviewJobService
    {
        public async Task ScanAndSendNotificationAsync(int days = 30)
        {
            var thresholdDate = DateTime.UtcNow.AddDays(-days);

            var itemsNeedReview = await _uow.ItemInventoryRepository.GetItemsNeedReviewWithDetailAsync(thresholdDate);

            if (!itemsNeedReview.Any())
                return;

            var notifications = new List<Notification>();

            foreach (var item in itemsNeedReview)
            {
                notifications.Add(new Notification
                {
                    UserId = item.UserId,

                    ItemInventoryId = item.Id,

                    TransactionDetailId = item.TransactionDetailId,

                    Message =$"Món {item.TransactionDetail.ItemName} cần đánh giá lại.",

                    IsRead = false,

                    Type = NotificationType.UsageReview,

                    CreatedAt = DateTime.UtcNow
                });
            }

            await _uow.NotificationRepository.AddRangeAsync(notifications);

            await _uow.Complete();
        }
    }
}