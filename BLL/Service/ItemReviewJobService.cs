using AutoMapper;
using BLL.Dtos;
using BLL.Interfaces.IServices;
using DAL.Entities;
using DAL.IRepositories;
using DAL.Enums;

namespace BLL.Service
{
    public class ItemReviewJobService(
        IUnitOfWork _uow,
        ISignalRNotificationService _signalRNotificationService,
        IMapper _mapper) : IItemReviewJobService
    {
        public async Task ScanAndSendNotificationAsync(int days = 30)
        {
            var thresholdDate = DateTime.UtcNow.AddHours(7).AddDays(-days);

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

                    Message = $"Món {item.TransactionDetail.ItemName} cần đánh giá lại.",

                    IsRead = false,

                    Type = NotificationType.UsageReview,

                    CreatedAt = DateTime.UtcNow.AddHours(7)
                });
            }

            await _uow.NotificationRepository.AddRangeAsync(notifications);

            // Lưu tất cả notifications vào DB trước
            await _uow.Complete();

            // Sau khi lưu xong, gửi real-time SignalR notification cho từng user
            var notificationDtos = _mapper.Map<IEnumerable<NotificationDto>>(notifications);
            var sendTasks = notificationDtos.Select(dto =>
                _signalRNotificationService.SendNotificationAsync(dto.UserId, dto));

            await Task.WhenAll(sendTasks);
        }
    }
}