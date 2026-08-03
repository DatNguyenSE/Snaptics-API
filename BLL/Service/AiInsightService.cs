using BLL.Dtos;
using BLL.Interfaces.IServices;
using DAL.Enums;
using DAL.IRepositories;

namespace BLL.Service
{
    public class AiInsightService(IUnitOfWork _uow, INotificationService _notificationService, ISnsService _snsService, IAiService _aiService) : IAiInsightService
    {
        public async Task GenerateInsightsAsync(string userId)
        {
            await CheckSpendingSpike(userId);

            await CheckBudgetWarning(userId);

            await CheckCategoryInsight(userId);

            await CheckInventoryInsight(userId);
        }

        private async Task CheckSpendingSpike(string userId)
        {
            var today = DateTime.UtcNow.AddHours(7);

            var firstDayOfMonth =
                new DateTime(today.Year, today.Month, 1);

            var firstDayOfNextMonth =
                firstDayOfMonth.AddMonths(1);

            var firstDayOfPreviousMonth =
                firstDayOfMonth.AddMonths(-1);

            var currentMonthTransactions =
                await _uow.TransactionRepository
                    .GetCompletedTransactionsWithDetailsAsync(
                        userId,
                        firstDayOfMonth,
                        firstDayOfNextMonth);

            var previousMonthTransactions =
                await _uow.TransactionRepository
                    .GetCompletedTransactionsWithDetailsAsync(
                        userId,
                        firstDayOfPreviousMonth,
                        firstDayOfMonth);

            var currentSpent =
                currentMonthTransactions.Sum(x => x.TotalAmount);

            var previousSpent =
                previousMonthTransactions.Sum(x => x.TotalAmount);

            if (previousSpent <= 0)
                return;

            if (currentSpent <= previousSpent * 1.3m)
                return;

            var increasePercent =
                ((currentSpent - previousSpent)
                    / previousSpent) * 100;

            if (await HasNotificationTodayAsync(
                    userId,
                    NotificationType.Other,
                    "so với tháng trước"))
            {
                return;
            }

            var message = $"Chi tiêu tháng này tăng {increasePercent:F0}% so với tháng trước.";
            await _notificationService.CreateAsync(
                new NotificationDto
                {
                    UserId = userId,
                    Message = message,
                    Type = NotificationType.Other,
                    CreatedAt = DateTime.UtcNow.AddHours(7)
                });

            await _snsService.PublishAsync(
                "Snaptics Spending Alert",
                message);
        }

        private async Task CheckBudgetWarning(string userId)
        {
            var today = DateTime.UtcNow.AddHours(7);

            var firstDayOfMonth =
                new DateTime(today.Year, today.Month, 1);

            var firstDayOfNextMonth =
                firstDayOfMonth.AddMonths(1);

            var monthlyTransactions =
                await _uow.TransactionRepository
                    .GetCompletedTransactionsWithDetailsAsync(
                        userId,
                        firstDayOfMonth,
                        firstDayOfNextMonth);

            var totalSpent =
                monthlyTransactions.Sum(x => x.TotalAmount);

            var budget =
                (await _uow.BudgetRepository.GetByUserIdAsync(userId))
                .FirstOrDefault(x => x.IsActive);

            if (budget == null || budget.Amount <= 0)
                return;

            var usagePercent =
                (totalSpent / budget.Amount) * 100;

            if (usagePercent >= 100)
            {
                if (await HasNotificationTodayAsync(
                        userId,
                        NotificationType.Other,
                        "vượt ngân sách"))
                {
                    return;
                }

                var message = "Bạn đã vượt ngân sách tháng này.";

                await _notificationService.CreateAsync(
                    new NotificationDto
                    {
                        UserId = userId,
                        Message = message,
                        Type = NotificationType.Other,
                        CreatedAt = DateTime.UtcNow.AddHours(7)
                    });

                await _snsService.PublishAsync(
                    "Snaptics Budget Warning",
                    message);
            }
            else if (usagePercent >= 80)
            {
                if (await HasNotificationTodayAsync(
                        userId,
                        NotificationType.Other,
                        "sử dụng"))
                {
                    return;
                }

                var message = $"Bạn đã sử dụng {usagePercent:F0}% ngân sách tháng này.";

                await _notificationService.CreateAsync(
                    new NotificationDto
                    {
                        UserId = userId,
                        Message = message,
                        Type = NotificationType.Other,
                        CreatedAt = DateTime.UtcNow.AddHours(7)
                    });

                await _snsService.PublishAsync(
                    "Snaptics Budget Warning",
                    message);
            }
        }

        private async Task CheckCategoryInsight(string userId)
        {
            var today = DateTime.UtcNow.AddHours(7);

            var firstDayOfMonth =
                new DateTime(today.Year, today.Month, 1);

            var firstDayOfNextMonth =
                firstDayOfMonth.AddMonths(1);

            var monthlyTransactions =
                await _uow.TransactionRepository
                    .GetCompletedTransactionsWithDetailsAsync(
                        userId,
                        firstDayOfMonth,
                        firstDayOfNextMonth);

            var totalSpent =
                monthlyTransactions.Sum(t => t.TotalAmount);

            if (totalSpent <= 0)
                return;

            var biggestCategory = monthlyTransactions
                .SelectMany(t => t.TransactionDetails)
                .GroupBy(td => td.Category.Name)
                .Select(g => new
                {
                    Category = g.Key,
                    Total = g.Sum(x => x.Price * x.Quantity)
                })
                .OrderByDescending(x => x.Total)
                .FirstOrDefault();

            if (biggestCategory == null)
                return;

            var percent =
                biggestCategory.Total / totalSpent;

            if (percent < 0.5m)
                return;

            var message =
                $"{biggestCategory.Category} chiếm {(percent * 100):F0}% tổng chi tiêu tháng này.";

            if (await HasNotificationTodayAsync(
                    userId,
                    NotificationType.Other,
                    "chiếm"))
            {
                return;
            }

            await _notificationService.CreateAsync(
                new NotificationDto
                {
                    UserId = userId,
                    Message = message,
                    Type = NotificationType.Other,
                    CreatedAt = DateTime.UtcNow.AddHours(7)
                });
        }

        private async Task CheckInventoryInsight(string userId)
        {
            var items = await _uow.ItemInventoryRepository.GetByUserIdAsync(userId);
            if (items == null || !items.Any()) return;

            // Chặn spam notification
            if (await HasNotificationTodayAsync(userId, NotificationType.Other, "đồ đạc")) return;

            var sb = new System.Text.StringBuilder();
            foreach (var item in items.Where(x => x.IsReviewed))
            {
                sb.AppendLine($"- {item.TransactionDetail?.ItemName} (Giá: {item.TransactionDetail?.Price:N0}đ, Mức độ sử dụng: {item.UsageStatus})");
            }

            if (sb.Length == 0) return; // Chưa đánh giá cái nào

            var systemPrompt = @"Bạn là trợ lý AI phân tích đồ đạc của Snaptics. 
Dưới đây là danh sách đồ đạc của người dùng:
" + sb.ToString() + @"
Nhiệm vụ: 
Tìm những đồ 'Frequent' (Dùng thường xuyên), hãy phân tích và gợi ý các sản phẩm liên quan hoặc cùng thể loại (VD: có chuột gaming thì gợi ý bàn phím, màn hình đang sale...).
Viết RA DUY NHẤT MỘT CÂU THÔNG BÁO NGẮN GỌN (dưới 150 ký tự) để gợi ý cho người dùng. 
Lưu ý: Bắt buộc trong câu phải có từ 'đồ đạc' để hệ thống nhận diện (VD: Đồ đạc của bạn...).
Nếu không có gì đáng nhắc, hãy trả về chữ 'EMPTY'.";

            try
            {
                var message = await _aiService.GenerateTextAsync(systemPrompt, "Hãy viết thông báo");
                message = message?.Trim();

                if (!string.IsNullOrEmpty(message) && message != "EMPTY" && !message.StartsWith("EMPTY") && message.Contains("đồ đạc", StringComparison.OrdinalIgnoreCase))
                {
                    await _notificationService.CreateAsync(
                        new NotificationDto
                        {
                            UserId = userId,
                            Message = message,
                            Type = NotificationType.Other,
                            CreatedAt = DateTime.UtcNow
                        });
                }
            }
            catch
            {
                // Ignore AI errors in background
            }
        }

        private async Task<bool> HasNotificationTodayAsync(string userId, NotificationType type, string keyword)
        {
            var notifications =
                await _uow.NotificationRepository.GetByUserIdAsync(userId);

            return notifications.Any(x =>
                x.Type == type &&
                x.Message.Contains(
                    keyword,
                    StringComparison.OrdinalIgnoreCase) &&
                x.CreatedAt.Date == DateTime.UtcNow.AddHours(7).Date);
        }
    }
}