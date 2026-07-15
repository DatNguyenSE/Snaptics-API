using BLL.AI;
using BLL.Dtos;
using BLL.Dtos.AiAssistantDto;
using BLL.Interfaces.IServices;
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
        IConfiguration _config,
        ITransactionService _transactionService)
        : IAiAssistantService
    {
        public async Task<AskAiResponseDto> AskAsync(
            string userId,
            AskAiRequestDto request)
        {
            var systemPrompt = PromptBuilder.Build();
            var responseJson = await CallGeminiAsync(systemPrompt, request.Message);
            var jsonNode = JsonNode.Parse(responseJson);
            
            // Lấy ra nội dung phản hồi từ mảng candidates của Gemini
            var partsNode = jsonNode?["candidates"]?[0]?["content"]?["parts"]?[0];

            if (partsNode?["functionCall"] != null)
            {
                var functionCall = partsNode["functionCall"];
                var functionName = functionCall?["name"]?.ToString();
                var args = functionCall?["args"];
                
                if (args == null)
                {
                    return new AskAiResponseDto { Reply = "😅 Ối, có chút lỗi kỹ thuật khi phân tích lệnh rồi. Bạn thử lại nhé!" };
                }

                if (functionName == "add_transaction")
                {
                    decimal amount = (decimal?)args["amount"] ?? 0;
                    string category = args["category"]?.ToString() ?? "Other";
                    string note = args["note"]?.ToString() ?? "Giao dịch AI";

                    var dto = new CreateTransactionWithDetailsDto
                    {
                        UserId = userId,
                        TotalAmount = amount,
                        TransactionDate = DateTime.UtcNow,
                        MerchantName = "AI Assistant",
                        Note = note,
                        Items = new List<CreateTransactionDetailItemDto>
                        {
                            new CreateTransactionDetailItemDto
                            {
                                ItemName = note,
                                Price = amount,
                                Quantity = 1,
                                Category = category
                            }
                        }
                    };

                    await _transactionService.CreateWithDetailsAsync(dto);
                    
                    // Câu trả lời thân thiện dựa theo ngữ cảnh
                    string reply = GetFriendlyReply(category, amount, note);

                    return new AskAiResponseDto { Reply = reply };
                }
                else if (functionName == "query_financial")
                {
                    string timeRange = args["time_range"]?.ToString() ?? "this_month";
                    
                    DateTime startDate, endDate;
                    var today = DateTime.UtcNow;
                    string timeFriendly = "tháng này";

                    switch (timeRange)
                    {
                        case "this_year":
                            startDate = new DateTime(today.Year, 1, 1);
                            endDate = startDate.AddYears(1);
                            timeFriendly = $"năm {today.Year}";
                            break;
                        case "last_month":
                            startDate = new DateTime(today.Year, today.Month, 1).AddMonths(-1);
                            endDate = startDate.AddMonths(1);
                            timeFriendly = "tháng trước";
                            break;
                        case "this_month":
                        default:
                            startDate = new DateTime(today.Year, today.Month, 1);
                            endDate = startDate.AddMonths(1);
                            timeFriendly = "tháng này";
                            break;
                    }

                    var transactions = await _uow.TransactionRepository
                        .GetCompletedTransactionsWithDetailsAsync(userId, startDate, endDate);
                    
                    var totalSpent = transactions.Sum(t => t.TotalAmount);
                    var count = transactions.Count();

                    string reply = count == 0 
                        ? $"🔍 Trống không! Trong {timeFriendly}, bạn chưa có giao dịch nào cả. Hãy chi tiêu và ghi chép lại nhé! 📝"
                        : $"📊 Báo cáo tài chính đây ạ: Trong {timeFriendly}, bạn đã thực hiện **{count}** giao dịch với tổng chi tiêu là **{totalSpent:N0}đ**. Hãy tiếp tục chi tiêu hợp lý nha! 💡";

                    return new AskAiResponseDto { Reply = reply };
                }
            }
            
            // Text response fallback của Gemini
            return new AskAiResponseDto
            {
                Reply = partsNode?["text"]?.ToString() ?? "AI không trả về kết quả."
            };
        }

        private async Task<string> CallGeminiAsync(string systemPrompt, string userMessage)
        {
            // Sử dụng trực tiếp cấu hình Gemini của hệ thống
            var apiKey = _config["AiSettings:GeminiApiKey"]
                ?? throw new InvalidOperationException("Thiếu AiSettings:GeminiApiKey trong cấu hình");
                
            var modelName = _config["AiSettings:GeminiModel"] ?? "gemini-1.5-flash";
            var apiVersion = _config["AiSettings:GeminiApiVersion"] ?? "v1beta";

            var endpoint = $"https://generativelanguage.googleapis.com/{apiVersion}/models/{modelName}:generateContent";

            // URL có truyền API Key
            var requestUrl = $"{endpoint}?key={apiKey}";

            var payload = new
            {
                system_instruction = new
                {
                    parts = new[] { new { text = systemPrompt } }
                },
                contents = new[]
                {
                    new
                    {
                        role = "user",
                        parts = new[] { new { text = userMessage } }
                    }
                },
                tools = new[]
                {
                    new
                    {
                        function_declarations = new object[]
                        {
                            new
                            {
                                name = "add_transaction",
                                description = "Thêm một giao dịch chi tiêu hoặc thu nhập mới.",
                                parameters = new
                                {
                                    type = "OBJECT",
                                    properties = new
                                    {
                                        amount = new { type = "NUMBER", description = "Số tiền" },
                                        category = new { type = "STRING", description = "Danh mục bằng tiếng Anh, VD: Food, Transport, Shopping, Other..." },
                                        note = new { type = "STRING", description = "Mô tả ngắn gọn" }
                                    },
                                    required = new[] { "amount", "category", "note" }
                                }
                            },
                            new
                            {
                                name = "query_financial",
                                description = "Truy vấn tổng chi tiêu theo thời gian.",
                                parameters = new
                                {
                                    type = "OBJECT",
                                    properties = new
                                    {
                                        time_range = new { type = "STRING", description = "Khoảng thời gian (this_month, last_month, this_year)" }
                                    },
                                    required = new[] { "time_range" }
                                }
                            }
                        }
                    }
                }
            };

            // Gửi thẳng vào Endpoint (không nối chuỗi query)
            var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
            
            // Cấp quyền bằng Header theo chuẩn của Google Gemini
            request.Headers.Add("x-goog-api-key", apiKey);
            
            // Gemini API dùng Content-Type application/json
            request.Content = new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json");

            var client = _httpClientFactory.CreateClient();
            var response = await client.SendAsync(request);
            var responseString = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Gemini API Error: {response.StatusCode}\n{responseString}");
            }

            return responseString;
        }

        private string GetFriendlyReply(string category, decimal amount, string note)
        {
            var random = new Random();
            string lowerCategory = category.ToLower();

            if (lowerCategory.Contains("food") || lowerCategory.Contains("eat") || lowerCategory.Contains("ăn"))
            {
                if (amount < 200000)
                {
                    var replies = new[] {
                        $"✅ Đã ghi nhận khoản **{note} ({amount:N0}đ)**. Ăn uống nạp năng lượng là chuẩn bài rồi! 🍜✨",
                        $"Đã trừ **{amount:N0}đ** tiền **{note}**. Chúc bạn bữa ăn ngon miệng nhé! 😋"
                    };
                    return replies[random.Next(replies.Length)];
                }
                else if (amount < 1000000)
                {
                    var replies = new[] {
                        $"Wow, bữa nay ăn uống thịnh soạn ghê! Đã ghi nhận khoản **{note} ({amount:N0}đ)** nha. 🍣🥂",
                        $"Đã lưu khoản **{note} ({amount:N0}đ)**. Ăn ngon mặc đẹp nhưng nhớ để ý hầu bao chút nha sếp! 🫣💸"
                    };
                    return replies[random.Next(replies.Length)];
                }
                else
                {
                    var replies = new[] {
                        $"Đỉnh quá! Bữa ăn **{note}** hết **{amount:N0}đ** luôn. Chắc là một dịp đặc biệt lắm đây! Đã ghi sổ nhé. 🎉🦞",
                        $"Ting ting! Đã trừ **{amount:N0}đ** tiền **{note}**. Ăn uống xả láng, ráng cày lại bù vào nha! 😱🔥"
                    };
                    return replies[random.Next(replies.Length)];
                }
            }
            else if (lowerCategory.Contains("travel") || lowerCategory.Contains("electronic") || lowerCategory.Contains("shopping"))
            {
                if (amount < 2000000)
                {
                    var replies = new[] {
                        $"✅ Đã lưu khoản **{note} ({amount:N0}đ)** vào mục **{category}**. Chi tiêu vui vẻ nhé! 🛒✨",
                        $"Đã ghi nhận khoản **{note} ({amount:N0}đ)**. Khoản này hoàn toàn trong tầm kiểm soát! ✌️"
                    };
                    return replies[random.Next(replies.Length)];
                }
                else if (amount < 10000000)
                {
                    var replies = new[] {
                        $"Chơi lớn luôn! Đã trừ **{amount:N0}đ** cho **{note}**. Lâu lâu tự thưởng cho bản thân cũng xứng đáng mà! ✈️🛍️",
                        $"Đã ghi sổ khoản **{note} ({amount:N0}đ)**. Khoản chi này hơi to xíu, tháng này nhớ thắt lưng buộc bụng phần ăn uống nha! 😅📉"
                    };
                    return replies[random.Next(replies.Length)];
                }
                else
                {
                    var replies = new[] {
                        $"Trời ơi, đại gia đây rồi! Khoản **{note}** lên tới **{amount:N0}đ**. Đã lưu cẩn thận vào sổ cho sếp! 👑💎",
                        $"Xác nhận trừ **{amount:N0}đ** cho **{note}**! Một khoản chi cực khủng, chúc bạn có trải nghiệm tuyệt vời với số tiền này nhé! 🚀🔥"
                    };
                    return replies[random.Next(replies.Length)];
                }
            }
            else
            {
                // Default category
                if (amount < 500000)
                {
                    var replies = new[] {
                        $"okie nha sếp! Khoản **{amount:N0}đ** cho **{note}** đã được lưu gọn gàng vào ví. 💳",
                        $"✅ Đã xong! Mình vừa ghi sổ khoản **{note} ({amount:N0}đ)** vào danh mục **{category}** rồi nhé. 📝✨"
                    };
                    return replies[random.Next(replies.Length)];
                }
                else
                {
                    var replies = new[] {
                        $"Ting ting! Đã trừ **{amount:N0}đ** tiền **{note}**. Số tiền khá lớn, ráng cân đối ngân sách nhé! 🫣📉",
                        $"Đã ghi nhận khoản lớn: **{note} ({amount:N0}đ)**. Mình đã đưa vào báo cáo tháng này rồi ạ! 📊"
                    };
                    return replies[random.Next(replies.Length)];
                }
            }
        }
    }
}