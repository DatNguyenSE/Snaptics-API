using BLL.Interfaces.IServices;
using DAL.Entities;
using DAL.IRepositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DAL.Enums;

namespace BLL.Service
{
    public interface IMissingPriceJob
    {
        Task ScanAndSendNotificationAsync();
    }

    public class MissingPriceJob(
        ITransactionService _transactionService, 
        IUnitOfWork _uow
    ) : IMissingPriceJob
    {
        public async Task ScanAndSendNotificationAsync()
        {
            var today = DateTime.Today;

            // 1. Lấy bill chưa xác nhận giá (IsAiEstimated = false)
            var missingTransactionsDto = await _transactionService.GetUnconfirmedTransactionsByDateAsync(today);

            if (!missingTransactionsDto.Any()) return;

            // 2. Gom nhóm theo người dùng
            var missingPriceReport = missingTransactionsDto
                .GroupBy(t => t.UserId)
                .Select(g => new
                {
                    UserId = g.Key,
                    MissingPriceCount = g.Count()
                })
                .ToList();

            // 3. Đẻ thông báo
            var newNotifications = new List<Notification>();
            foreach (var report in missingPriceReport)
            {
                newNotifications.Add(new Notification
                {
                    UserId = report.UserId,
                    Message = $"Bạn có {report.MissingPriceCount} hóa đơn chưa cập nhật giá hôm nay. Nhấn vào đây để xem và xác nhận nhé!",
                    IsRead = false,
                    Type = NotificationType.MissingInfo,
                    CreatedAt = DateTime.UtcNow
                });
            }

            // 4. Dùng UnitOfWork để lưu vào bảng Notification
            await _uow.NotificationRepository.AddRangeAsync(newNotifications);
            await _uow.Complete();
        }
    }
}