using BLL.Dtos.AiAssistantDto;
using System.Text;

namespace BLL.AI
{
    public static class PromptBuilder
    {
        public static string Build(
            FinancialContextDto context,
            string question)
        {
            var prompt = new StringBuilder();

            prompt.AppendLine("Bạn là AI Financial Assistant của Snaptics.");

            prompt.AppendLine();

            prompt.AppendLine("Chỉ được trả lời dựa trên dữ liệu được cung cấp.");

            prompt.AppendLine("Nếu thiếu dữ liệu hãy nói rõ.");

            prompt.AppendLine();

            prompt.AppendLine("===== Thông tin tài chính =====");

            prompt.AppendLine($"Tổng tiền tháng này: {context.TotalSpentThisMonth}");

            prompt.AppendLine($"Số giao dịch: {context.TotalTransactionsThisMonth}");

            prompt.AppendLine($"Danh mục chi nhiều nhất: {context.TopSpendingCategory}");

            if (context.AllCategoriesThisMonth.Any())
            {
                prompt.AppendLine($"Các danh mục đã chi tiêu: {string.Join(", ", context.AllCategoriesThisMonth)}");
            }

            prompt.AppendLine($"Số món cần review: {context.NeedReviewCount}");

            prompt.AppendLine($"Số giao dịch thiếu giá: {context.MissingPriceCount}");

            if (context.NeedReviewItems.Any())
            {
                prompt.AppendLine(
                    $"Review items: {string.Join(", ", context.NeedReviewItems)}");
            }

            prompt.AppendLine();

            prompt.AppendLine("===== User Question =====");

            prompt.AppendLine(question);

            return prompt.ToString();
        }
    }
}