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
        IMapper _mapper,
        ICategoryService _categoryService) : IItemReviewJobService
    {
        public async Task ScanAndSendNotificationAsync(int days = 30)
        {
            await TriggerScanAndSendNotificationAsync(days);
        }

        public async Task<int> TriggerScanAndSendNotificationAsync(int days = 30)
        {
            var thresholdDate = DateTime.UtcNow.AddHours(7).AddDays(-days);

            // BƯỚC 1: ĐỒNG BỘ ITEM INVENTORY
            // Quét các TransactionDetail cũ hơn thresholdDate mà chưa có ItemInventory
            var missingDetails = await _uow.TransactionDetailRepository.GetDetailsWithoutInventoryAsync(thresholdDate);
            
            var newInventories = new List<ItemInventory>();
            foreach (var detail in missingDetails)
            {
                if (await _categoryService.GetEffectiveInventoryTrackingAsync(detail.CategoryId, detail.Transaction.UserId))
                {
                    newInventories.Add(new ItemInventory
                    {
                        UserId = detail.Transaction.UserId,
                        TransactionDetailId = detail.Id,
                        UsageStatus = UsageStatusType.NotEvaluated,
                        IsReviewed = false,
                        CreatedAt = detail.Transaction.TransactionDate
                    });
                }
            }

            if (newInventories.Any())
            {
                await _uow.ItemInventoryRepository.AddRangeAsync(newInventories);
                await _uow.Complete(); // Lưu lại để bước 2 có thể quét thấy các record này
            }

            // BƯỚC 2: QUÉT VÀ GỬI THÔNG BÁO ĐÁNH GIÁ
            var itemsNeedReview = await _uow.ItemInventoryRepository.GetItemsNeedReviewWithDetailAsync(thresholdDate);

            if (!itemsNeedReview.Any())
                return 0;

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

            return notifications.Count;
        }
    }
}