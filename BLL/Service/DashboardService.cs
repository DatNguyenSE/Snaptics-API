using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BLL.Dtos;
using BLL.Interfaces.IServices;
using DAL.Data;
using DAL.Entities;
using DAL.Enums;
using Microsoft.EntityFrameworkCore;

namespace BLL.Service
{
    public class DashboardService : IDashboardService
    {
        private readonly AppDbContext _context; 

        public DashboardService(AppDbContext context)
        {
            _context = context;
        }
        public async Task<DashboardResponseDto> GetDashboardSummaryAsync(string userId, DateTime fromDate, DateTime toDate)
        {
            var startDate = fromDate.Date;
            var endDateExclusive = toDate.Date.AddDays(1);

            // 1. Lọc giao dịch của User trong khoảng thời gian và nạp sẵn detail + category để tổng hợp dashboard
            var query = _context.Transactions
                .Include(t => t.TransactionDetails)
                .ThenInclude(td => td.Category)
                .Where(t => t.UserId == userId && t.TransactionDate >= startDate && t.TransactionDate < endDateExclusive);

            var transactions = await query.ToListAsync();
            var response = new DashboardResponseDto();

            // 2. Tính thẻ Tổng quan từ Transaction
            response.TotalIncome = transactions
                .Where(t => !t.IsExpense)
                .SelectMany(t => t.TransactionDetails)
                .Sum(td => td.Price * td.Quantity);

            response.TotalExpense = transactions
                .Where(t => t.IsExpense)
                .SelectMany(t => t.TransactionDetails)
                .Sum(td => td.Price * td.Quantity);

            response.Balance = response.TotalIncome - response.TotalExpense;

            // 3. Biểu đồ tròn (Cơ cấu chi tiêu - chỉ tính Expense)
            response.PieChart = transactions
                .Where(t => t.IsExpense)
                .SelectMany(t => t.TransactionDetails)
                .GroupBy(td => td.Category?.Name ?? "Unknown")
                .Select(g => new PieChartDto
                {
                    CategoryName = g.Key,
                    TotalAmount = g.Sum(td => td.Price * td.Quantity)
                })
                .OrderByDescending(x => x.TotalAmount)
                .ToList();

            // 4. Biểu đồ cột (Biến hóa theo thời gian)
            double daysDiff = (endDateExclusive - startDate).TotalDays;

            if (daysDiff <= 1)
            {
                // Xem theo NGÀY -> Cột là các GIỜ
                response.BarChart = BuildHourlyBarChart(transactions, startDate);
            }
            else if (daysDiff <= 31)
            {
                // Xem theo TUẦN/THÁNG -> Cột là các NGÀY
                response.BarChart = BuildDailyBarChart(transactions, startDate, endDateExclusive);
            }
            else
            {
                // Xem theo NĂM -> Cột là các THÁNG
                response.BarChart = BuildMonthlyBarChart(transactions, startDate, endDateExclusive);
            }

            return response;
        }

        public async Task<CategorySummaryResponseDto> GetCategorySummaryAsync(string userId, DateTime fromDate, DateTime toDate)
        {
            var startDate = fromDate.Date;
            var endDateExclusive = toDate.Date.AddDays(1);

            var query = _context.Transactions
                .Include(t => t.TransactionDetails)
                .ThenInclude(td => td.Category)
                .Where(t => t.UserId == userId && t.TransactionDate >= startDate && t.TransactionDate < endDateExclusive);

            var transactions = await query.ToListAsync();

            var groupedCategories = transactions
                .Where(t => t.IsExpense)
                .SelectMany(t => t.TransactionDetails)
                .GroupBy(td => td.Category?.Name ?? "Khác")
                .Select(g => new CategorySummaryItemDto
                {
                    Name = g.Key,
                    TotalAmount = g.Sum(td => td.Price * td.Quantity)
                })
                .OrderByDescending(x => x.TotalAmount)
                .ToList();

            var totalAmountAll = groupedCategories.Sum(x => x.TotalAmount);

            foreach (var item in groupedCategories)
            {
                item.Percentage = totalAmountAll > 0 ? Math.Round((item.TotalAmount / totalAmountAll) * 100, 2) : 0;
            }

            var response = new CategorySummaryResponseDto
            {
                Breakdown = groupedCategories,
                TopCategory = groupedCategories.FirstOrDefault()
            };

            return response;
        }

        public async Task<List<BarChartDto>> GetTrendSummaryAsync(string userId, DateTime fromDate, DateTime toDate)
        {
            var startDate = fromDate.Date;
            var endDateExclusive = toDate.Date.AddDays(1);

            var query = _context.Transactions
                .Include(t => t.TransactionDetails)
                .ThenInclude(td => td.Category)
                .Where(t => t.UserId == userId && t.TransactionDate >= startDate && t.TransactionDate < endDateExclusive);

            var transactions = await query.ToListAsync();

            double daysDiff = (endDateExclusive - startDate).TotalDays;

            if (daysDiff <= 1)
            {
                return BuildHourlyBarChart(transactions, startDate);
            }
            else if (daysDiff <= 31)
            {
                return BuildDailyBarChart(transactions, startDate, endDateExclusive);
            }
            else
            {
                return BuildMonthlyBarChart(transactions, startDate, endDateExclusive);
            }
        }

        public async Task<SpendingComparisonDto> GetSpendingComparisonAsync(string userId)
        {
            var now = DateTime.Now;
            
            // Week calculations
            int diff = (7 + (now.DayOfWeek - DayOfWeek.Monday)) % 7;
            var currentWeekStart = now.Date.AddDays(-1 * diff);
            var previousWeekStart = currentWeekStart.AddDays(-7);
            
            // Month calculations
            var currentMonthStart = new DateTime(now.Year, now.Month, 1);
            var previousMonthStart = currentMonthStart.AddMonths(-1);
            
            // Year calculations
            var currentYearStart = new DateTime(now.Year, 1, 1);
            var previousYearStart = currentYearStart.AddYears(-1);
            
            // The earliest date we need is previousYearStart
            var earliestDate = previousYearStart;
            var endDateExclusive = now.Date.AddDays(1);

            var query = _context.Transactions
                .Include(t => t.TransactionDetails)
                .Where(t => t.UserId == userId && t.IsExpense && !t.IsDeleted && t.TransactionDate >= earliestDate && t.TransactionDate < endDateExclusive);

            var transactions = await query.ToListAsync();

            decimal GetAmount(DateTime start, DateTime endExclusive)
            {
                return transactions
                    .Where(t => t.TransactionDate >= start && t.TransactionDate < endExclusive)
                    .SelectMany(t => t.TransactionDetails)
                    .Sum(td => td.Price * td.Quantity);
            }

            var currentWeekAmount = GetAmount(currentWeekStart, currentWeekStart.AddDays(7));
            var previousWeekAmount = GetAmount(previousWeekStart, currentWeekStart);

            var currentMonthAmount = GetAmount(currentMonthStart, currentMonthStart.AddMonths(1));
            var previousMonthAmount = GetAmount(previousMonthStart, currentMonthStart);

            var currentYearAmount = GetAmount(currentYearStart, currentYearStart.AddYears(1));
            var previousYearAmount = GetAmount(previousYearStart, currentYearStart);

            SpendingPeriodData CreateData(decimal current, decimal previous)
            {
                decimal percentage = 0;
                if (previous > 0)
                {
                    percentage = Math.Round(((current - previous) / previous) * 100, 2);
                }
                else if (current > 0)
                {
                    percentage = 100;
                }

                return new SpendingPeriodData
                {
                    CurrentAmount = current,
                    PreviousAmount = previous,
                    PercentageChange = percentage,
                    IsBetter = current <= previous
                };
            }

            return new SpendingComparisonDto
            {
                Week = CreateData(currentWeekAmount, previousWeekAmount),
                Month = CreateData(currentMonthAmount, previousMonthAmount),
                Year = CreateData(currentYearAmount, previousYearAmount)
            };
        }

        private List<BarChartDto> BuildHourlyBarChart(List<Transaction> transactions, DateTime dayStart)
        {
            var aggregates = transactions
                .GroupBy(t => t.TransactionDate.Hour)
                .ToDictionary(
                    g => g.Key,
                    g => new
                    {
                        Income = g.Sum(x => GetTransactionAmount(x, false)),
                        Expense = g.Sum(x => GetTransactionAmount(x, true))
                    });

            var result = new List<BarChartDto>();
            for (var hour = 0; hour < 24; hour++)
            {
                aggregates.TryGetValue(hour, out var value);
                result.Add(new BarChartDto
                {
                    Label = $"{hour:00}:00",
                    Income = value?.Income ?? 0m,
                    Expense = value?.Expense ?? 0m
                });
            }

            return result;
        }

        private List<BarChartDto> BuildDailyBarChart(List<Transaction> transactions, DateTime startDate, DateTime endDateExclusive)
        {
            var aggregates = transactions
                .GroupBy(t => t.TransactionDate.Date)
                .ToDictionary(
                    g => g.Key,
                    g => new
                    {
                        Income = g.Sum(x => GetTransactionAmount(x, false)),
                        Expense = g.Sum(x => GetTransactionAmount(x, true))
                    });

            var result = new List<BarChartDto>();
            for (var date = startDate; date < endDateExclusive; date = date.AddDays(1))
            {
                aggregates.TryGetValue(date, out var value);
                result.Add(new BarChartDto
                {
                    Label = date.ToString("dd/MM"),
                    Income = value?.Income ?? 0m,
                    Expense = value?.Expense ?? 0m
                });
            }

            return result;
        }

        private List<BarChartDto> BuildMonthlyBarChart(List<Transaction> transactions, DateTime startDate, DateTime endDateExclusive)
        {
            var aggregates = transactions
                .GroupBy(t => new DateTime(t.TransactionDate.Year, t.TransactionDate.Month, 1))
                .ToDictionary(
                    g => g.Key,
                    g => new
                    {
                        Income = g.Sum(x => GetTransactionAmount(x, false)),
                        Expense = g.Sum(x => GetTransactionAmount(x, true))
                    });

            var result = new List<BarChartDto>();
            var monthCursor = new DateTime(startDate.Year, startDate.Month, 1);
            var lastMonth = new DateTime(endDateExclusive.AddDays(-1).Year, endDateExclusive.AddDays(-1).Month, 1);

            while (monthCursor <= lastMonth)
            {
                aggregates.TryGetValue(monthCursor, out var value);
                result.Add(new BarChartDto
                {
                    Label = $"Tháng {monthCursor.Month}",
                    Income = value?.Income ?? 0m,
                    Expense = value?.Expense ?? 0m
                });

                monthCursor = monthCursor.AddMonths(1);
            }

            return result;
        }

        private static decimal GetTransactionAmount(Transaction transaction, bool isExpense)
        {
            if (transaction.IsExpense != isExpense) return 0m;
            return transaction.TransactionDetails
                .Sum(td => td.Price * td.Quantity);
        }
    }
}