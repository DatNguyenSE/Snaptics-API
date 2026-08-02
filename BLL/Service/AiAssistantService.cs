using BLL.AI;
using BLL.Dtos;
using BLL.Dtos.AiAssistantDto;
using BLL.Interfaces.IServices;
using BLL.Exceptions;
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
                    string? dateStr = args["date"]?.ToString();
                    
                    var nowInVietnam = DateTime.UtcNow.AddHours(7);
                    DateTime transactionDate = nowInVietnam;

                    if (!string.IsNullOrEmpty(dateStr) && DateTime.TryParse(dateStr, out DateTime parsedDate))
                    {
                        if (parsedDate.TimeOfDay == TimeSpan.Zero)
                        {
                            // Nếu Gemini chỉ trả về ngày YYYY-MM-DD (chưa có giờ), kết hợp Ngày từ ngữ cảnh + Giờ/Phút hiện tại
                            transactionDate = parsedDate.Date.Add(nowInVietnam.TimeOfDay);
                        }
                        else
                        {
                            // Nếu người dùng có nói rõ giờ (vd: 8h tối qua), dùng chính xác ngày giờ đó
                            transactionDate = parsedDate;
                        }
                    }

                    string? walletName = args["wallet_name"]?.ToString();
                    int? targetBudgetId = null;
                    DAL.Entities.Budget? matchedBudget = null;

                    if (!string.IsNullOrWhiteSpace(walletName))
                    {
                        var normalizedWalletName = walletName.Trim().ToLower();

                        var ownBudgets = (await _uow.BudgetRepository.GetByUserIdAsync(userId)).ToList();

                        var sharedBudgetMembers = await _uow.BudgetMemberRepository.GetSharedBudgetsByUserIdAsync(userId);
                        
                        var validSharedBudgets = sharedBudgetMembers
                            .Where(bm => bm.Status == DAL.Enums.InvitationStatus.Accepted 
                                      && bm.Role == DAL.Enums.BudgetRole.Editor)
                            .Select(bm => bm.Budget)
                            .ToList();

                        var allValidBudgets = ownBudgets.Concat(validSharedBudgets).ToList();

                        matchedBudget = allValidBudgets.FirstOrDefault(b => 
                            b != null && b.Name != null && b.Name.ToLower().Contains(normalizedWalletName));

                        if (matchedBudget != null)
                        {
                            targetBudgetId = matchedBudget.Id;
                        }
                        else
                        {
                            return new AskAiResponseDto 
                            { 
                                Reply = $"❌ Giao dịch chưa được lưu! Mình không tìm thấy ví nào tên là '{walletName}'. Bạn hãy kiểm tra lại nhé." 
                            };
                        }
                    }

                    bool isExpense = true;
                    if (args["is_expense"] != null && bool.TryParse(args["is_expense"]!.ToString(), out bool parsedIsExpense))
                    {
                        isExpense = parsedIsExpense;
                    }

                    var dto = new CreateTransactionWithDetailsDto
                    {
                        BudgetId = targetBudgetId,
                        UserId = userId,
                        TotalAmount = amount,
                        TransactionDate = transactionDate,
                        MerchantName = note,
                        Note = "Nhập nhanh",
                        IsExpense = isExpense,
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

                    string? usedWalletName = matchedBudget?.Name;
                    if (string.IsNullOrEmpty(usedWalletName))
                    {
                        var ownBudgets = await _uow.BudgetRepository.GetByUserIdAsync(userId);
                        usedWalletName = ownBudgets.FirstOrDefault(b => b.IsDefault)?.Name ?? "mặc định";
                    }

                    await _transactionService.CreateWithDetailsAsync(dto);
                    
                    // Câu trả lời thân thiện dựa theo ngữ cảnh
                    string reply = GetFriendlyReply(category, amount, note, usedWalletName, isExpense);

                    return new AskAiResponseDto { Reply = reply };
                }
                else if (functionName == "query_financial")
                {
                    string timeRange = args["time_range"]?.ToString() ?? "this_month";
                    
                    DateTime startDate, endDate;
                    var today = DateTime.UtcNow.AddHours(7);
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

                    // Tách riêng thu nhập và chi tiêu (không tính chung)
                    var totalExpense = transactions.Where(t => t.IsExpense).Sum(t => t.TotalAmount);
                    var totalIncome = transactions.Where(t => !t.IsExpense).Sum(t => t.TotalAmount);
                    var balance = totalIncome - totalExpense;
                    var expenseCount = transactions.Count(t => t.IsExpense);
                    var incomeCount = transactions.Count(t => !t.IsExpense);

                    string reply;
                    if (transactions.Count() == 0)
                    {
                        reply = $"🔍 Trống không! Trong {timeFriendly}, bạn chưa có giao dịch nào cả. Hãy chi tiêu và ghi chép lại nhé! 📝";
                    }
                    else if (totalIncome == 0)
                    {
                        reply = $"📊 Báo cáo tài chính đây ạ! Trong {timeFriendly}, bạn chi tiêu tổng cộng **{totalExpense:N0}đ** ({expenseCount} khoản) mà chưa ghi nhận khoản thu nhập nào. Hãy tiếp tục chi tiêu hợp lý nha! 💡";
                    }
                    else if (totalExpense == 0)
                    {
                        reply = $"📊 Báo cáo tài chính đây ạ! Trong {timeFriendly}, bạn nhận được tổng cộng **{totalIncome:N0}đ** ({incomeCount} khoản thu nhập) và chưa chi tiêu khoản nào. Tuyệt vời! 💰✨";
                    }
                    else if (balance < 0)
                    {
                        reply = $"📊 Báo cáo tài chính đây ạ! Trong {timeFriendly}:\n" +
                                $"💰 Thu nhập: **{totalIncome:N0}đ** ({incomeCount} khoản)\n" +
                                $"💸 Chi tiêu: **{totalExpense:N0}đ** ({expenseCount} khoản)\n" +
                                $"⚠️ Kết quả: bạn chi **nhiều hơn thu {Math.Abs(balance):N0}đ**. Cần cân đối lại ngân sách một chút nha! 📉";
                    }
                    else
                    {
                        reply = $"📊 Báo cáo tài chính đây ạ! Trong {timeFriendly}:\n" +
                                $"💰 Thu nhập: **{totalIncome:N0}đ** ({incomeCount} khoản)\n" +
                                $"💸 Chi tiêu: **{totalExpense:N0}đ** ({expenseCount} khoản)\n" +
                                $"✅ Kết quả: bạn dư **{balance:N0}đ**. Rất tốt, hãy tiếp tục chi tiêu hợp lý nha! 💡";
                    }

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
                
            var modelName = _config["AiSettings:GeminiModel"] ?? "gemini-flash-lite-latest";
            var apiVersion = _config["AiSettings:GeminiApiVersion"] ?? "v1beta";

            var endpoint = $"https://generativelanguage.googleapis.com/{apiVersion}/models/{modelName}:generateContent";

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
                                        category = new { type = "STRING", description = "Danh mục giao dịch. Phải dùng ĐÚNG một trong các tên sau: 'Ăn uống', 'Đồ uống', 'Di chuyển', 'Điện tử', 'Gia dụng', 'Quần áo & Phụ kiện', 'Sức khỏe & Làm đẹp', 'Giải trí', 'Giáo dục', 'Quà tặng & Thưởng', 'Hóa đơn & Tiện ích', 'Đầu tư & Tiết kiệm', 'Khác'. Ví dụ: 'được mẹ thưởng 50k', 'nhận quà', 'bố cho tiền' -> 'Quà tặng & Thưởng'. Nếu không chắc thì chọn 'Khác'." },
                                        note = new { type = "STRING", description = "Mô tả ngắn gọn" },
                                        date = new { type = "STRING", description = "Ngày/giờ thực hiện giao dịch (định dạng YYYY-MM-DD hoặc YYYY-MM-DD HH:mm:ss nếu người dùng nói rõ giờ cụ thể)." },
                                        wallet_name = new { type = "STRING", description = "Tên ví/nguồn tiền (ví dụ: momo, tiền ăn sinh hoạt, tiền mặt). Loại bỏ từ phụ như 'ở ví', 'trừ vào ví'. Trả về null nếu không có." },
                                        is_expense = new { type = "BOOLEAN", description = "true nếu là khoản chi tiêu/trừ tiền (ăn uống, mua đồ...), false nếu là khoản thu nhập/nhận tiền/cộng tiền (được thưởng, nhận lương, quà tặng...)." }
                                    },
                                    required = new[] { "amount", "category", "note", "date", "is_expense" }
                                }
                            },
                            new
                            {
                                name = "query_financial",
                                description = "Truy vấn tổng thu nhập, tổng chi tiêu và số dư theo thời gian (tách riêng tiền thu và tiền chi, không tính chung).",
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

            var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
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
                throw new GeminiApiException(
                    $"Gemini API Error: {response.StatusCode} | Model: {modelName}\n{responseString}",
                    response.StatusCode);
            }

            return responseString;
        }

        private string GetFriendlyReply(string category, decimal amount, string note, string walletName, bool isExpense = true)
        {
            var random = new Random();

            if (!isExpense)
            {
                var incomeReplies = new[] {
                    $"🎉 Tuyệt vời! Đã cộng **{amount:N0}đ** ({note}) vào ví **\"{walletName}\"**. Thu nhập rủng rỉnh nha! 💰✨",
                    $"✅ Đã ghi nhận khoản thu nhập **{note} (+{amount:N0}đ)** vào ví **\"{walletName}\"**. Ting ting! 💸🚀"
                };
                return incomeReplies[random.Next(incomeReplies.Length)];
            }

            string lowerCategory = category.ToLower();

            if (lowerCategory.Contains("food") || lowerCategory.Contains("eat") || lowerCategory.Contains("ăn"))
            {
                if (amount < 200000)
                {
                    var replies = new[] {
                        $"✅ Đã trừ **{amount:N0}đ** tiền **{note}** vào ví **\"{walletName}\"**. Ăn uống nạp năng lượng là chuẩn bài rồi! 🍜✨",
                        $"Đã trừ **{amount:N0}đ** tiền **{note}** vào ví **\"{walletName}\"**. Chúc bạn bữa ăn ngon miệng nhé! 😋"
                    };
                    return replies[random.Next(replies.Length)];
                }
                else if (amount < 1000000)
                {
                    var replies = new[] {
                        $"Wow, bữa nay ăn uống thịnh soạn ghê! Đã trừ **{amount:N0}đ** tiền **{note}** vào ví **\"{walletName}\"** nha. 🍣🥂",
                        $"Đã trừ **{amount:N0}đ** tiền **{note}** vào ví **\"{walletName}\"**. Ăn ngon mặc đẹp nhưng nhớ để ý hầu bao chút nha sếp! 🫣💸"
                    };
                    return replies[random.Next(replies.Length)];
                }
                else
                {
                    var replies = new[] {
                        $"Đỉnh quá! Bữa ăn **{note}** hết **{amount:N0}đ** đã được trừ vào ví **\"{walletName}\"**. Chắc là một dịp đặc biệt lắm đây! 🎉🦞",
                        $"Ting ting! Đã trừ **{amount:N0}đ** tiền **{note}** vào ví **\"{walletName}\"**. Ăn uống xả láng, ráng cày lại bù vào nha! 😱🔥"
                    };
                    return replies[random.Next(replies.Length)];
                }
            }
            else if (lowerCategory.Contains("travel") || lowerCategory.Contains("electronic") || lowerCategory.Contains("shopping"))
            {
                if (amount < 2000000)
                {
                    var replies = new[] {
                        $"✅ Đã trừ **{amount:N0}đ** cho **{note}** vào ví **\"{walletName}\"** (Danh mục: **{category}**). Chi tiêu vui vẻ nhé! 🛒✨",
                        $"Đã trừ **{amount:N0}đ** tiền **{note}** vào ví **\"{walletName}\"**. Khoản này hoàn toàn trong tầm kiểm soát! ✌️"
                    };
                    return replies[random.Next(replies.Length)];
                }
                else if (amount < 10000000)
                {
                    var replies = new[] {
                        $"Chơi lớn luôn! Đã trừ **{amount:N0}đ** cho **{note}** vào ví **\"{walletName}\"**. Lâu lâu tự thưởng cho bản thân cũng xứng đáng mà! ✈️🛍️",
                        $"Đã ghi sổ khoản **{note} ({amount:N0}đ)** vào ví **\"{walletName}\"**. Khoản chi này hơi to xíu, tháng này nhớ thắt lưng buộc bụng nha! 😅📉"
                    };
                    return replies[random.Next(replies.Length)];
                }
                else
                {
                    var replies = new[] {
                        $"Trời ơi, đại gia đây rồi! Khoản **{note}** lên tới **{amount:N0}đ** đã trừ vào ví **\"{walletName}\"**. 👑💎",
                        $"Xác nhận trừ **{amount:N0}đ** cho **{note}** vào ví **\"{walletName}\"**! Một khoản chi cực khủng! 🚀🔥"
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
                        $"okie nha sếp! Đã trừ **{amount:N0}đ** tiền **{note}** vào ví **\"{walletName}\"**. 💳",
                        $"✅ Đã xong! Mình vừa ghi sổ khoản **{note} ({amount:N0}đ)** vào ví **\"{walletName}\"** (Danh mục: **{category}**) rồi nhé. 📝✨"
                    };
                    return replies[random.Next(replies.Length)];
                }
                else
                {
                    var replies = new[] {
                        $"Ting ting! Đã trừ **{amount:N0}đ** tiền **{note}** vào ví **\"{walletName}\"**. Số tiền khá lớn, ráng cân đối ngân sách nhé! 🫣📉",
                        $"Đã ghi nhận trừ **{amount:N0}đ** cho **{note}** vào ví **\"{walletName}\"**. Mình đã đưa vào báo cáo tháng này rồi ạ! 📊"
                    };
                    return replies[random.Next(replies.Length)];
                }
            }
        }
    }
}
