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

        public async Task<byte[]> ExportInventoryInsightCsvAsync()
        {
            var allItems = await _uow.ItemInventoryRepository.GetAllWithDetailsAsync();

            var filteredItems = allItems
                .Where(x => x.UsageStatus == UsageStatusType.Frequent || x.UsageStatus == UsageStatusType.Occasionally)
                .ToList();

            if (!filteredItems.Any())
            {
                return new byte[] { 0xEF, 0xBB, 0xBF }; // Empty CSV with BOM
            }

            var groupedOriginals = filteredItems
                .Where(x => x.TransactionDetail != null && x.TransactionDetail.Category != null)
                .GroupBy(x => new { x.TransactionDetail.ItemName, CategoryName = x.TransactionDetail.Category.Name })
                .Select(g => new
                {
                    ItemName = g.Key.ItemName,
                    CategoryName = g.Key.CategoryName,
                    Count = g.Count()
                })
                .ToList();

            var uniqueNames = groupedOriginals.Select(x => x.ItemName).Distinct().ToList();

            var systemPrompt = @"Bạn là trợ lý dữ liệu. Dưới đây là danh sách tên các món đồ. Hãy gom nhóm các tên tương tự nhau thành 1 TÊN CHUNG DUY NHẤT (Generic Name). Ví dụ: 'chuột gaming', 'chuột không dây' -> 'chuột máy tính'. Trả về MỘT mảng JSON duy nhất chứa đối tượng có 'OriginalName' và 'GenericName'. Không có văn bản nào khác ngoài JSON.
            [
              { ""OriginalName"": ""..."", ""GenericName"": ""..."" }
            ]
            
            Danh sách món đồ:
            " + string.Join(", ", uniqueNames);

            var aiResponse = await _aiService.GenerateTextAsync(systemPrompt, "Hãy trả về JSON.");
            
            var genericNameMappings = new Dictionary<string, string>();
            try
            {
                aiResponse = aiResponse?.Trim();
                if (!string.IsNullOrEmpty(aiResponse))
                {
                    if (aiResponse.StartsWith("```json")) aiResponse = aiResponse.Substring(7);
                    if (aiResponse.EndsWith("```")) aiResponse = aiResponse.Substring(0, aiResponse.Length - 3);
                    aiResponse = aiResponse.Trim();

                    var mappings = System.Text.Json.JsonSerializer.Deserialize<List<BLL.Dtos.AiAssistantDto.AiGenericNameDto>>(aiResponse);
                    if (mappings != null)
                    {
                        foreach (var m in mappings)
                        {
                            if (!string.IsNullOrEmpty(m.OriginalName) && !genericNameMappings.ContainsKey(m.OriginalName))
                            {
                                genericNameMappings[m.OriginalName] = m.GenericName ?? m.OriginalName;
                            }
                        }
                    }
                }
            }
            catch
            {
                // Fallback: If AI fails, we just map everything to its original name
            }

            var finalGrouped = groupedOriginals
                .Select(x => new
                {
                    GenericName = genericNameMappings.ContainsKey(x.ItemName) ? genericNameMappings[x.ItemName] : x.ItemName,
                    CategoryName = x.CategoryName,
                    Count = x.Count
                })
                .GroupBy(x => new { x.GenericName, x.CategoryName })
                .Select(g => new
                {
                    CategoryName = g.Key.CategoryName,
                    GenericName = g.Key.GenericName,
                    TotalCount = g.Sum(x => x.Count)
                })
                .OrderBy(x => x.CategoryName)
                .ThenByDescending(x => x.TotalCount)
                .ToList();

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("Category,Generic Name,Count");
            foreach (var item in finalGrouped)
            {
                var safeCategory = item.CategoryName?.Replace("\"", "\"\"") ?? "";
                var safeGenericName = item.GenericName?.Replace("\"", "\"\"") ?? "";
                sb.AppendLine($"\"{safeCategory}\",\"{safeGenericName}\",{item.TotalCount}");
            }

            var csvBytes = System.Text.Encoding.UTF8.GetBytes(sb.ToString());
            var bom = new byte[] { 0xEF, 0xBB, 0xBF };
            return bom.Concat(csvBytes).ToArray();
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