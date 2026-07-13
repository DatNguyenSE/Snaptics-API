using BLL.Dtos.AiAssistantDto;
using System.Text;

namespace BLL.AI
{
    public static class PromptBuilder
    {
        public static string Build()
        {
            var prompt = new StringBuilder();

            prompt.AppendLine("Bạn là trợ lý tài chính thông minh của ứng dụng Snaptics.");
            prompt.AppendLine("Nhiệm vụ của bạn là hỗ trợ người dùng ghi chép chi tiêu và tra cứu thống kê.");
            prompt.AppendLine("Khi người dùng yêu cầu ghi chép (ví dụ: 'ăn trưa 300k'), hãy sử dụng tool add_transaction.");
            prompt.AppendLine("Khi người dùng hỏi số liệu (ví dụ: 'tháng này tiêu bao nhiêu'), hãy sử dụng tool query_financial.");
            prompt.AppendLine("Nếu câu hỏi không liên quan đến tài chính, bạn hãy giao tiếp thân thiện như một chatbot bình thường.");

            return prompt.ToString();
        }
    }
}