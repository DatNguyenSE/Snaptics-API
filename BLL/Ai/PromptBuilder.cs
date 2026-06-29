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

            prompt.AppendLine($"Tổng tiền tháng trước: {context.PreviousMonthSpent}");

            prompt.AppendLine($"Số giao dịch: {context.TotalTransactionsThisMonth}");

            prompt.AppendLine($"Danh mục chi nhiều nhất: {context.TopSpendingCategory}");

            prompt.AppendLine($"Món tốn tiền nhất: {context.TopSpendingItem}");

            prompt.AppendLine($"Số tiền của món tốn nhất: {context.TopSpendingItemAmount}");

            if (context.AllCategoriesThisMonth.Any())
            {
                prompt.AppendLine(
                    $"Các danh mục đã chi tiêu: {string.Join(", ", context.AllCategoriesThisMonth)}");
            }

            if (context.CategorySpendings.Any())
            {
                prompt.AppendLine();
                prompt.AppendLine("Chi tiêu theo danh mục:");

                foreach (var category in context.CategorySpendings)
                {
                    prompt.AppendLine(
                        $"- {category.CategoryName}: {category.TotalAmount}");
                }
            }

            if (context.TopExpenses.Any())
            {
                prompt.AppendLine();
                prompt.AppendLine("Các khoản chi lớn:");

                foreach (var expense in context.TopExpenses)
                {
                    prompt.AppendLine(
                        $"- {expense.ItemName}: {expense.Amount}");
                }
            }

            prompt.AppendLine();

            prompt.AppendLine($"Số món cần review: {context.NeedReviewCount}");

            prompt.AppendLine($"Số giao dịch thiếu giá: {context.MissingPriceCount}");

            if (context.NeedReviewItems.Any())
            {
                prompt.AppendLine(
                    $"Review items: {string.Join(", ", context.NeedReviewItems)}");
            }

            prompt.AppendLine();

            prompt.AppendLine("===== Quy tắc trả lời =====");

            prompt.AppendLine(
                "- Nếu người dùng hỏi chi bao nhiêu cho một danh mục, hãy dùng dữ liệu 'Chi tiêu theo danh mục'.");

            prompt.AppendLine(
                "- Nếu người dùng hỏi món nào tốn tiền nhất, hãy dùng 'Món tốn tiền nhất'.");

            prompt.AppendLine(
                "- Nếu người dùng hỏi tháng này có chi nhiều hơn tháng trước không, hãy so sánh 'Tổng tiền tháng này' và 'Tổng tiền tháng trước'.");

            prompt.AppendLine(
                "- Trả lời ngắn gọn, rõ ràng, bằng tiếng Việt.");

            prompt.AppendLine();

            prompt.AppendLine("===== Câu hỏi người dùng =====");

            prompt.AppendLine(question);

            return prompt.ToString();
        }
    }
}