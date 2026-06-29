using BLL.AI;
using BLL.Dtos.AiAssistantDto;
using BLL.Interfaces.IServices;
using DAL.Enums;
using DAL.IRepositories;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Configuration;

namespace BLL.Service
{
    public class AiAssistantService(
        IUnitOfWork _uow,
        IHttpClientFactory _httpClientFactory,
        IConfiguration _config)
        : IAiAssistantService
    {
        public async Task<AskAiResponseDto> AskAsync(
            string userId,
            AskAiRequestDto request)
        {
            // Build user's financial context
            var context = await BuildFinancialContextAsync(userId);

            var prompt = PromptBuilder.Build(context, request.Message);

            var reply = await CallOpenAiAsync(prompt);

            // TODO: Build Prompt

            // TODO: Call AI Model

            return new AskAiResponseDto
            {
                Reply = reply
            };
        }

        private async Task<FinancialContextDto> BuildFinancialContextAsync(string userId)
        {
            var context = new FinancialContextDto();

            await LoadTransactionSummaryAsync(context, userId);

            await LoadReviewSummaryAsync(context, userId);

            await LoadMissingPriceSummaryAsync(context, userId);

            return context;
        }

        private async Task LoadTransactionSummaryAsync(FinancialContextDto context, string userId)
        {
            var today = DateTime.UtcNow;

            var firstDayOfMonth = new DateTime(today.Year, today.Month, 1);
            var firstDayOfNextMonth = firstDayOfMonth.AddMonths(1);
            var firstDayOfPreviousMonth = firstDayOfMonth.AddMonths(-1);

            var monthlyTransactions = await _uow.TransactionRepository.GetCompletedTransactionsWithDetailsAsync(
                                                userId,
                                                firstDayOfMonth,
                                                firstDayOfNextMonth);

            var previousMonthTransactions = await _uow.TransactionRepository
                                                .GetCompletedTransactionsWithDetailsAsync(
                                                userId,
                                                firstDayOfPreviousMonth,
                                                firstDayOfMonth);

            context.PreviousMonthSpent = previousMonthTransactions.Sum(t => t.TotalAmount);

            context.TotalSpentThisMonth = monthlyTransactions.Sum(t => t.TotalAmount);

            context.TotalTransactionsThisMonth = monthlyTransactions.Count();

            var topCategory = monthlyTransactions
                .SelectMany(t => t.TransactionDetails)
                .GroupBy(td => td.Category.Name)
                .Select(g => new
                {
                    Category = g.Key,
                    Total = g.Sum(x => x.Price * x.Quantity)
                })
                .OrderByDescending(x => x.Total)
                .FirstOrDefault();

            context.TopSpendingCategory = topCategory?.Category;

            context.AllCategoriesThisMonth = monthlyTransactions
                .SelectMany(t => t.TransactionDetails)
                .Select(td => td.Category.Name)
                .Distinct()
                .ToList();

            var biggestItem = monthlyTransactions
                .SelectMany(t => t.TransactionDetails)
                .OrderByDescending(x => x.Price * x.Quantity)
                .FirstOrDefault();

            if (biggestItem != null)
            {
                context.TopSpendingItem = biggestItem.ItemName;

                context.TopSpendingItemAmount =
                    biggestItem.Price * biggestItem.Quantity;
            }

            context.CategorySpendings = monthlyTransactions
                .SelectMany(t => t.TransactionDetails)
                .GroupBy(td => td.Category.Name)
                .Select(g => new CategorySpendingDto
                {CategoryName = g.Key, TotalAmount = g.Sum(x => x.Price * x.Quantity)})
                .ToList();

            context.TopExpenses = monthlyTransactions
                .SelectMany(t => t.TransactionDetails)
                .OrderByDescending(x => x.Price * x.Quantity)
                .Take(5)
                .Select(x => new TopExpenseDto
                {ItemName = x.ItemName, Amount = x.Price * x.Quantity})
                .ToList();
        }

        private async Task LoadReviewSummaryAsync(FinancialContextDto context, string userId)
        {
            var pendingReviewItems =
                await _uow.ItemInventoryRepository
                    .GetNeedReviewItemsAsync(userId);

            context.NeedReviewCount =
                pendingReviewItems.Count();

            context.NeedReviewItems =
                pendingReviewItems
                    .Select(x => x.TransactionDetail.ItemName)
                    .ToList();
        }

        private async Task<string> CallOpenAiAsync(string prompt)
        {
            var endpoint = _config["AiModel:Endpoint"]
                ?? throw new InvalidOperationException("Thiếu AiModel:Endpoint");

            var apiKey = _config["AiModel:ApiKey"]
                ?? throw new InvalidOperationException("Thiếu AiModel:ApiKey");

            var modelName = _config["AiModel:ModelName"] ?? "gpt-4o-mini";

            var payload = new
            {
                model = modelName,
                messages = new[]
                {
            new
            {
                role = "user",
                content = prompt
            }
        },
                temperature = 0.3
            };

            var request = new HttpRequestMessage(HttpMethod.Post, endpoint);

            request.Headers.Add("Authorization", $"Bearer {apiKey}");

            request.Content = new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json");

            var client = _httpClientFactory.CreateClient();

            var response = await client.SendAsync(request);

            var responseString = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception(
                    $"AI Error: {response.StatusCode}\n{responseString}");
            }

            var jsonNode = JsonNode.Parse(responseString);

            return jsonNode?["choices"]?[0]?["message"]?["content"]?.ToString()
                   ?? "AI không trả về kết quả.";
        }

        private async Task LoadMissingPriceSummaryAsync(FinancialContextDto context, string userId)
        {
            var missingTransactions =
                await _uow.TransactionRepository.FindAsync(t =>
                    t.UserId == userId &&
                    !t.IsDeleted &&
                    t.Status == TransactionStatusType.MissingPrice);

            context.MissingPriceCount = missingTransactions.Count();
        }
    }
}