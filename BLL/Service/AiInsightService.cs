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

            var message = $"📈 Chi tiêu tháng này của bạn đã tăng {increasePercent:F0}% so với tháng trước. Hãy lưu ý nhé!";
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

                var message = "⚠️ Bạn đã vượt quá ngân sách tháng này rồi. Hãy cẩn thận nha!";

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

                var message = $"⚠️ Bạn đã sử dụng tới {usagePercent:F0}% ngân sách tháng này.";

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
                $"📊 Danh mục '{biggestCategory.Category}' đang chiếm tới {(percent * 100):F0}% tổng chi tiêu tháng này. Bạn hãy chú ý cân đối ngân sách nhé!";

            if (await HasNotificationTodayAsync(
                    userId,
                    NotificationType.Other,
                    "chiếm tới"))
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
            if (await HasNotificationTodayAsync(userId, NotificationType.Other, "Gợi ý đồ đạc:")) return;

            var sb = new System.Text.StringBuilder();
            foreach (var item in items.Where(x => x.IsReviewed))
            {
                sb.AppendLine($"- {item.TransactionDetail?.ItemName} (Giá: {item.TransactionDetail?.Price:N0}đ, Mức độ sử dụng: {item.UsageStatus})");
            }

            if (sb.Length == 0) return; // Chưa đánh giá cái nào

            var systemPrompt = @"Bạn là chuyên gia tư vấn mua sắm thông minh của Snaptics. 
Dưới đây là danh sách các món đồ người dùng đã mua và mức độ sử dụng của họ:
" + sb.ToString() + @"
Nhiệm vụ: 
1. Phân tích các món đồ có mức độ sử dụng 'Frequent' (Dùng thường xuyên).
2. Dựa vào đó, gợi ý NGẮN GỌN 1-2 món đồ liên quan hoặc phụ kiện hữu ích mà người dùng có thể mua thêm để nâng cao trải nghiệm (Ví dụ: có điện thoại -> gợi ý ốp lưng, sạc dự phòng).
3. KHÔNG chào hỏi, KHÔNG giải thích lằng nhằng. CHỈ viết DUY NHẤT một câu gợi ý thân thiện (khoảng 15 - 25 từ).
4. Nếu danh sách không có đồ 'Frequent' hoặc không có gợi ý nào hợp lý, HÃY TRẢ VỀ ĐÚNG 1 CHỮ: EMPTY";

            try
            {
                var message = await _aiService.GenerateTextAsync(systemPrompt, "Hãy viết câu gợi ý mua sắm:");
                message = message?.Trim();

                if (!string.IsNullOrEmpty(message) && message != "EMPTY" && !message.StartsWith("EMPTY"))
                {
                    var finalMessage = $"📦 Gợi ý đồ đạc: {message}";
                    await _notificationService.CreateAsync(
                        new NotificationDto
                        {
                            UserId = userId,
                            Message = finalMessage,
                            Type = NotificationType.Other,
                            CreatedAt = DateTime.UtcNow.AddHours(7)
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