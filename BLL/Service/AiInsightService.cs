using BLL.Dtos;
using BLL.Interfaces.IServices;
using DAL.Enums;
using DAL.IRepositories;

namespace BLL.Service
{
    public class AiInsightService(
        IUnitOfWork _uow,
        INotificationService _notificationService)
        : IAiInsightService
    {
        public async Task GenerateInsightsAsync(string userId)
        {
            await CheckSpendingSpike(userId);

            await CheckBudgetWarning(userId);

            await CheckCategoryInsight(userId);
        }

        private async Task CheckSpendingSpike(string userId)
        {
            var today = DateTime.UtcNow;

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

            await _notificationService.CreateAsync(
                new NotificationDto
                {
                    UserId = userId,
                    Message =
                        $"Chi tiêu tháng này tăng {increasePercent:F0}% so với tháng trước.",
                    Type = NotificationType.Other,
                    CreatedAt = DateTime.UtcNow
                });
        }

        private async Task CheckBudgetWarning(string userId)
        {
            var today = DateTime.UtcNow;

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
                        "ngân sách"))
                {
                    return;
                }

                await _notificationService.CreateAsync(
                    new NotificationDto
                    {
                        UserId = userId,
                        Message =
                            "Bạn đã vượt ngân sách tháng này.",
                        Type = NotificationType.Other,
                        CreatedAt = DateTime.UtcNow
                    });
            }
            else if (usagePercent >= 80)
            {
                if (await HasNotificationTodayAsync(
                        userId,
                        NotificationType.Other,
                        "ngân sách"))
                {
                    return;
                }

                await _notificationService.CreateAsync(
                    new NotificationDto
                    {
                        UserId = userId,
                        Message =
                            $"Bạn đã sử dụng {usagePercent:F0}% ngân sách tháng này.",
                        Type = NotificationType.Other,
                        CreatedAt = DateTime.UtcNow
                    });
            }
        }

        private async Task CheckCategoryInsight(string userId)
        {
            var today = DateTime.UtcNow;

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
                    CreatedAt = DateTime.UtcNow
                });
        }

        private async Task<bool> HasNotificationTodayAsync(
            string userId,
            NotificationType type,
            string keyword)
        {
            var notifications =
                await _uow.NotificationRepository.GetByUserIdAsync(userId);

            return notifications.Any(x =>
                x.Type == type &&
                x.Message.Contains(keyword) &&
                x.CreatedAt.Date == DateTime.UtcNow.Date);
        }
    }
}