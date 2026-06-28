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

            // 2. Tính thẻ Tổng quan từ TransactionDetails theo CategoryType
            response.TotalIncome = transactions
                .SelectMany(t => t.TransactionDetails)
                .Where(td => td.Category != null && td.Category.Type == CategoryType.Income)
                .Sum(td => td.Price * td.Quantity);

            response.TotalExpense = transactions
                .SelectMany(t => t.TransactionDetails)
                .Where(td => td.Category != null && td.Category.Type == CategoryType.Expense)
                .Sum(td => td.Price * td.Quantity);

            response.Balance = response.TotalIncome - response.TotalExpense;

            // 3. Biểu đồ tròn (Cơ cấu chi tiêu - chỉ tính Expense)
            response.PieChart = transactions
                .SelectMany(t => t.TransactionDetails)
                .Where(td => td.Category != null && td.Category.Type == CategoryType.Expense)
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

        private List<BarChartDto> BuildHourlyBarChart(List<Transaction> transactions, DateTime dayStart)
        {
            var aggregates = transactions
                .GroupBy(t => t.TransactionDate.Hour)
                .ToDictionary(
                    g => g.Key,
                    g => new
                    {
                        Income = g.Sum(x => GetTransactionAmountByType(x, CategoryType.Income)),
                        Expense = g.Sum(x => GetTransactionAmountByType(x, CategoryType.Expense))
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
                        Income = g.Sum(x => GetTransactionAmountByType(x, CategoryType.Income)),
                        Expense = g.Sum(x => GetTransactionAmountByType(x, CategoryType.Expense))
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
                        Income = g.Sum(x => GetTransactionAmountByType(x, CategoryType.Income)),
                        Expense = g.Sum(x => GetTransactionAmountByType(x, CategoryType.Expense))
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

        private static decimal GetTransactionAmountByType(Transaction transaction, CategoryType categoryType)
        {
            return transaction.TransactionDetails
                .Where(td => td.Category != null && td.Category.Type == categoryType)
                .Sum(td => td.Price * td.Quantity);
        }
    }
}