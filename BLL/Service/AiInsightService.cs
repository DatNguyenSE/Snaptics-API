using BLL.Dtos;
using BLL.Interfaces.IServices;
using DAL.Enums;
using DAL.IRepositories;

namespace BLL.Service
{
    public class AiInsightService(IUnitOfWork _uow, INotificationService _notificationService, ISnsService _snsService, IAiService _aiService) : IAiInsightService
    {
        public async Task<int> GenerateInsightsAsync(string userId)
        {
            int count = 0;
            count += await CheckSpendingSpike(userId);
            count += await CheckBudgetWarning(userId);
            count += await CheckCategoryInsight(userId);
            return count;
        }

        public async Task<int> GenerateInventoryInsightAsync(string userId)
        {
            return await CheckInventoryInsight(userId);
        }

        private async Task<int> CheckSpendingSpike(string userId)
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
                return 0;

            if (currentSpent <= previousSpent * 1.3m)
                return 0;

            var increasePercent =
                ((currentSpent - previousSpent)
                    / previousSpent) * 100;

            if (await HasNotificationTodayAsync(
                    userId,
                    NotificationType.Other,
                    "so với tháng trước"))
            {
                return 0;
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
                
            return 1;
        }

        private async Task<int> CheckBudgetWarning(string userId)
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
                return 0;

            var usagePercent =
                (totalSpent / budget.Amount) * 100;

            if (usagePercent >= 100)
            {
                if (await HasNotificationTodayAsync(
                        userId,
                        NotificationType.Other,
                        "vượt ngân sách"))
                {
                    return 0;
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
                    
                return 1;
            }
            else if (usagePercent >= 80)
            {
                if (await HasNotificationTodayAsync(
                        userId,
                        NotificationType.Other,
                        "sử dụng"))
                {
                    return 0;
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
                    
                return 1;
            }
            return 0;
        }

        private async Task<int> CheckCategoryInsight(string userId)
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
                return 0;

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
                return 0;

            var percent =
                biggestCategory.Total / totalSpent;

            if (percent < 0.5m)
                return 0;

            var message =
                $"📊 Danh mục '{biggestCategory.Category}' đang chiếm tới {(percent * 100):F0}% tổng chi tiêu tháng này. Bạn hãy chú ý cân đối ngân sách nhé!";

            if (await HasNotificationTodayAsync(
                    userId,
                    NotificationType.Other,
                    "chiếm tới"))
            {
                return 0;
            }

            await _notificationService.CreateAsync(
                new NotificationDto
                {
                    UserId = userId,
                    Message = message,
                    Type = NotificationType.Other,
                    CreatedAt = DateTime.UtcNow.AddHours(7)
                });
                
            return 1;
        }

        private async Task<int> CheckInventoryInsight(string userId)
        {
            var items = await _uow.ItemInventoryRepository.GetByUserIdAsync(userId);
            if (items == null || !items.Any()) return 0;

            // Chặn spam notification
            if (await HasNotificationTodayAsync(userId, NotificationType.Other, "Gợi ý đồ đạc:")) return 0;

            var sb = new System.Text.StringBuilder();
            foreach (var item in items.Where(x => x.IsReviewed))
            {
                sb.AppendLine($"- {item.TransactionDetail?.ItemName} (Giá: {item.TransactionDetail?.Price:N0}đ, Mức độ sử dụng: {item.UsageStatus})");
            }

            if (sb.Length == 0) return 0; // Chưa đánh giá cái nào

            var systemPrompt = @"Bạn là chuyên gia tư vấn mua sắm và quản lý đồ đạc thông minh của Snaptics. 
Dưới đây là danh sách các món đồ người dùng đã mua và mức độ sử dụng của họ:
" + sb.ToString() + @"
Nhiệm vụ: 
1. Đối với đồ có mức độ sử dụng là 'Frequent' (luôn dùng): Gợi ý thêm 1 sản phẩm cùng thể loại. Bạn phải chèn 1 câu tin nhắn đúng định dạng sau: ""Hôm nay sản phẩm [Tên sản phẩm gợi ý] giảm giá sâu bạn có muốn xem không? [bấm vào đây để truy cập link]([link])""
   Trong đó [link] là một trong 3 nền tảng sau (chọn ngẫu nhiên):
   - Shopee: https://shopee.vn/search?keyword=<tên_món_gợi_ý>
   - Lazada: https://www.lazada.vn/catalog/?q=<tên_món_gợi_ý>
   - TikTok Shop: https://www.tiktok.com/search?q=<tên_món_gợi_ý>
   (Nhớ thay khoảng trắng bằng %20 trong link).
   Ví dụ định dạng chuẩn: Hôm nay sản phẩm Ốp lưng iPhone 15 giảm giá sâu bạn có muốn xem không? [bấm vào đây để truy cập link](https://shopee.vn/search?keyword=%E1%BB%90p%20l%C6%B0ng%20iPhone%2015)
2. Đối với đồ 'Unused' hoặc 'Seldom': Khuyên họ thanh lý (pass đồ), quyên góp để dọn dẹp không gian và 'thu hồi vốn' 💸.
3. Viết 1 đoạn văn lôi cuốn, đánh trúng tâm lý thích mua sắm hoặc tiết kiệm của người dùng. KHÔNG chào hỏi vòng vo.
4. Nếu danh sách không có đồ nào phù hợp để gợi ý, HÃY TRẢ VỀ ĐÚNG 1 CHỮ: EMPTY";

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
                        
                    return 1;
                }
            }
            catch
            {
                // Ignore AI errors in background
            }
            
            return 0;
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