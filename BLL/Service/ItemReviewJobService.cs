using BLL.Interfaces.IServices;
using DAL.Entities;
using DAL.IRepositories;
using DAL.Enums;

namespace BLL.Service
{
    public class ItemReviewJobService(
        IItemInventoryService _itemInventoryService,IUnitOfWork _uow) : IItemReviewJobService
    {
        public async Task ScanAndSendNotificationAsync(int days = 30)
        {
            var itemsNeedReview =
                await _itemInventoryService.GetItemsNeedReviewAsync(days);

            if (!itemsNeedReview.Any())
                return;

            var groupedUsers = itemsNeedReview.GroupBy(x => x.UserId).Select(g => new
                {
                    UserId = g.Key,
                    Count = g.Count()
                })
                .ToList();

            var notifications =new List<Notification>();

            foreach (var user in groupedUsers)
            {
                notifications.Add(new Notification
                {
                    UserId = user.UserId,
                    Message =$"Bạn có {user.Count} món đồ cần review lại.",
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