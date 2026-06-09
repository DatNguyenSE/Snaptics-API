using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Azure;
using Azure.AI.FormRecognizer.DocumentAnalysis;
using BLL.Dtos.AiDto;
using BLL.Interfaces.IServices;
using DAL.Entities;
using DAL.IRepositories;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;

namespace BLL.Service
{
    public class AiService : IAiService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _config;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMemoryCache _cache;

        // Cache key cho toàn bộ từ điển items
        private const string DictionaryCacheKey = "ItemDictionary_All";
        private static readonly TimeSpan CacheExpiry = TimeSpan.FromMinutes(30);

        // ──────────────── Azure Doc Intelligence Config ────────────────
        private string AzureEndpoint => _config["AiSettings:AzureDocIntelEndpoint"]
            ?? throw new InvalidOperationException("AiSettings:AzureDocIntelEndpoint not found in configuration.");
        private string AzureKey => _config["AiSettings:AzureDocIntelKey"]
            ?? throw new InvalidOperationException("AiSettings:AzureDocIntelKey not found in configuration.");

        // Prompt mạnh mẽ gửi kèm ảnh phân tích
        private const string AnalyzeImagePrompt =
            "Đóng vai một chuyên gia thẩm định giá và chuyên gia dinh dưỡng. " +
            "Hãy phân tích hình ảnh này để xác định đây là món đồ hay thực phẩm gì. " +
            "CHÚ Ý: NẾU CÓ NHIỀU VẬT THỂ GIỐNG NHAU, HÃY ĐẾM SỐ LƯỢNG VÀ NHÂN TỔNG calo (nếu là đồ ăn) cùng tổng giá tiền thị trường lên. " +
            "Ví dụ 1: Nếu thấy 2 quả táo, itemName là 'Táo đỏ (2 quả)', category là 'Thực phẩm', estimatedCalories là tổng calo, estimatedPriceVND là tổng giá 2 quả. " +
            "Ví dụ 2: Nếu thấy 1 cái ghế, itemName là 'Ghế văn phòng', category là 'Đồ nội thất', estimatedCalories là 0, estimatedPriceVND là giá thị trường ước tính của cái ghế đó. " +
            "Quy tắc phân loại (category): Phân loại càng cụ thể càng tốt bằng tiếng Việt (ví dụ: Thực phẩm, Đồ nội thất, Đồ điện tử, Quần áo, Văn phòng phẩm, Mỹ phẩm, v.v.). Nếu không thể xác định, BẮT BUỘC để category là 'Unknown'. Không trả về chuỗi chung chung như 'Food/Object' hay 'Object'. " +
            "Trả về cho tôi một chuỗi JSON chuẩn có cấu trúc: " +
            "{ \"itemName\": \"\", \"category\": \"\", \"estimatedCalories\": 0, \"estimatedPriceVND\": 0 }. " +
            "Chỉ trả về JSON, không giải thích gì thêm. Không bọc kết quả trong markdown (như ```json ).";

        public AiService(IHttpClientFactory httpClientFactory, IConfiguration config,
            IUnitOfWork unitOfWork, IMemoryCache cache)
        {
            _httpClientFactory = httpClientFactory;
            _config = config;
            _unitOfWork = unitOfWork;
            _cache = cache;
        }

        // ═══════════════════════════════════════════════════════
        // Feature 1: Phân tích ảnh bằng AI (ChatGPT / gpt-4o-mini)
        // ═══════════════════════════════════════════════════════
        public async Task<AnalyzeImageResponseDto> AnalyzeImageAsync(IFormFile image)
        {
            var endpoint = _config["AiModel:Endpoint"] ?? "https://models.inference.ai.azure.com/chat/completions";
            var apiKey = _config["AiModel:ApiKey"] ?? throw new InvalidOperationException("Thiếu API Key của AiModel");
            var modelName = _config["AiModel:ModelName"] ?? "gpt-4o-mini";

            // 1. Kiểm tra tính hợp lệ của file ảnh đầu vào (không rỗng)
            if (image == null || image.Length == 0)
                throw new ArgumentException("File ảnh không hợp lệ hoặc rỗng.");

            var allowedTypes = new[] { "image/jpeg", "image/png", "image/webp", "image/heic" };
            if (!allowedTypes.Contains(image.ContentType.ToLower()))
                throw new ArgumentException($"Định dạng ảnh không hỗ trợ: {image.ContentType}. Hỗ trợ: jpg, png, webp, heic.");

            // 2. Đọc nội dung file ảnh và chuyển đổi sang chuỗi Base64
            string base64Image;
            using (var ms = new MemoryStream())
            {
                await image.CopyToAsync(ms);
                base64Image = Convert.ToBase64String(ms.ToArray());
            }

            // 3. Xây dựng Payload theo chuẩn OpenAI API.
            // Payload bao gồm Prompt (hướng dẫn AI đóng vai chuyên gia dinh dưỡng) và ảnh dạng Base64.
            var payload = new
            {
                model = modelName,
                messages = new[]
                {
                    new
                    {
                        role = "user",
                        content = new object[]
                        {
                            new { type = "text", text = AnalyzeImagePrompt },
                            new 
                            { 
                                type = "image_url", 
                                image_url = new { url = $"data:{image.ContentType};base64,{base64Image}" } 
                            }
                        }
                    }
                },
                response_format = new { type = "json_object" }, // ÉP KIỂU JSON
                temperature = 0.1
            };

            var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
            request.Headers.Add("Authorization", $"Bearer {apiKey}");
            request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            // 4. Thực hiện gửi HTTP POST request gọi API của LLM Provider
            var client = _httpClientFactory.CreateClient();
            var response = await client.SendAsync(request);
            var responseString = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Lỗi từ AI Provider: {response.StatusCode} - {responseString}");
            }

            // 5. Bóc tách lấy nội dung JSON từ cục response của AI
            var jsonNode = JsonNode.Parse(responseString);
            var aiTextResponse = jsonNode?["choices"]?[0]?["message"]?["content"]?.ToString();

            if (string.IsNullOrEmpty(aiTextResponse))
            {
                throw new Exception("AI không trả về kết quả hợp lệ.");
            }

            // 6. Chuyển đổi chuỗi JSON (Deserialize) sang các class DTO mong muốn
            try
            {
                // Deserialize thẳng ra AnalyzeImageResponseDto (hoặc qua DTO trung gian)
                var result = JsonSerializer.Deserialize<AiAnalyzeResult>(aiTextResponse, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }) ?? throw new Exception("Deserialize ra null.");

              
                

                return new AnalyzeImageResponseDto
                {
                    ItemName = result.ItemName,
                    Category = result.Category,
                    EstimatedCalories = result.EstimatedCalories,
                    EstimatedPriceVND = result.EstimatedPriceVND
                };
            }
            catch (JsonException ex)
            {
                throw new Exception($"Lỗi Parse JSON: {ex.Message}. Chuỗi AI: {aiTextResponse}");
            }
        }

        // ═══════════════════════════════════════════════════════
        // Feature 2: Đọc Bill bằng Azure Document Intelligence
        // ═══════════════════════════════════════════════════════
        public async Task<BillReadResultDto> ReadBillAsync(IFormFile billImage)
        {
            // 1. Kiểm tra tính hợp lệ của file hóa đơn
            if (billImage == null || billImage.Length == 0)
                throw new ArgumentException("File bill không hợp lệ hoặc rỗng.");

            var allowedTypes = new[] { "image/jpeg", "image/png", "image/tiff", "application/pdf" };
            if (!allowedTypes.Contains(billImage.ContentType.ToLower()))
                throw new ArgumentException($"Định dạng không hỗ trợ: {billImage.ContentType}. Hỗ trợ: jpg, png, tiff, pdf.");

            // 2. Khởi tạo Azure Document Intelligence client với Endpoint và API Key
            var credential = new AzureKeyCredential(AzureKey);
            var docClient = new DocumentAnalysisClient(new Uri(AzureEndpoint), credential);

            // 3. Gửi ảnh lên Azure để phân tích bằng mô hình chuyên dụng cho hóa đơn (prebuilt-receipt)
            AnalyzeDocumentOperation operation;
            using (var stream = billImage.OpenReadStream())
            {
                operation = await docClient.AnalyzeDocumentAsync(
                    WaitUntil.Completed,
                    "prebuilt-receipt",
                    stream,
                    new AnalyzeDocumentOptions { Locale = "vi-VN" }
                );
            }

            var analyzeResult = operation.Value;

            // 4. Bóc tách thông tin thô từ Azure thành dữ liệu có cấu trúc (Tên cửa hàng, Ngày, Tổng tiền, Món hàng...)
            var billResult = new BillReadResultDto();

            if (analyzeResult.Documents.Count > 0)
            {
                var receipt = analyzeResult.Documents[0];

                // Merchant Name
                if (receipt.Fields.TryGetValue("MerchantName", out var merchantField))
                    billResult.MerchantName = merchantField.Content;

                // Transaction Date — dùng .FieldType trên DocumentField (không phải .Value.ValueType)
                if (receipt.Fields.TryGetValue("TransactionDate", out var dateField)
                    && dateField.FieldType == DocumentFieldType.Date)
                    billResult.TransactionDate = dateField.Value.AsDate().DateTime;

                // Total Amount
                if (receipt.Fields.TryGetValue("Total", out var totalField)
                    && totalField.FieldType == DocumentFieldType.Double)
                    billResult.TotalAmount = (decimal)totalField.Value.AsDouble();

                // Currency
                if (receipt.Fields.TryGetValue("Currency", out var currencyField))
                    billResult.Currency = currencyField.Content ?? "VND";

                // Items
                if (receipt.Fields.TryGetValue("Items", out var itemsField)
                    && itemsField.FieldType == DocumentFieldType.List)
                {
                    foreach (var itemField in itemsField.Value.AsList())
                    {
                        // itemField là DocumentField — check FieldType trực tiếp
                        if (itemField.FieldType != DocumentFieldType.Dictionary) continue;

                        var itemDict = itemField.Value.AsDictionary();
                        var billItem = new BillItemDto();

                        if (itemDict.TryGetValue("Description", out var descField))
                            billItem.ItemName = descField.Content ?? "Unknown Item";

                        // Fix: Dùng decimal và parse tay từ Content để tránh lỗi locale vi-VN (hiểu nhầm 1.344 thành 1344)
                        if (itemDict.TryGetValue("Quantity", out var qtyField))
                        {
                            bool isParsed = false;
                            if (!string.IsNullOrWhiteSpace(qtyField.Content))
                            {
                                // Lọc chỉ lấy số, dấu chấm và phẩy từ chuỗi (vd: "1.344 kg" -> "1.344")
                                var numericStr = new string(qtyField.Content.Where(c => char.IsDigit(c) || c == '.' || c == ',').ToArray());
                                numericStr = numericStr.Replace(',', '.'); // Quy chuẩn về dấu chấm thập phân

                                if (decimal.TryParse(numericStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var parsedQty))
                                {
                                    billItem.Quantity = parsedQty;
                                    isParsed = true;
                                }
                            }

                            // Fallback về giá trị Double của Azure nếu parse tay thất bại
                            if (!isParsed && qtyField.FieldType == DocumentFieldType.Double)
                            {
                                billItem.Quantity = (decimal)qtyField.Value.AsDouble();
                            }
                        }

                        if (itemDict.TryGetValue("Price", out var priceField)
                            && priceField.FieldType == DocumentFieldType.Double)
                            billItem.Price = (decimal)priceField.Value.AsDouble();

                        if (!string.IsNullOrWhiteSpace(billItem.ItemName))
                            billResult.Items.Add(billItem);
                    }
                }
            }

            // 5. Đã trích xuất thành công danh sách các món hàng (Items).
            // Gọi AI (chỉ 1 lần duy nhất) để tự động phân loại tất cả mặt hàng thành Food hoặc Object, thay vì gọi nhiều lần.
            if (billResult.Items.Count > 0)
            {
                await EnrichItemCategoriesAsync(billResult.Items);
            }

            return billResult;
        }

        /// <summary>
        /// Phân loại items bằng pattern: Local Dictionary Cache → LLM Fallback → Self-learning.
        /// - Bước 1: Load từ điển từ DB vào MemoryCache (cache 30 phút).
        /// - Bước 2: So khớp từng item với cache. Items đã biết được gán category ngay.
        /// - Bước 3: Chỉ gửi những items CHƯA BIẾT lên LLM (1 request duy nhất).
        /// - Bước 4: Lưu kết quả mới từ LLM vào DB + invalidate cache (self-learning).
        /// </summary>
        private async Task EnrichItemCategoriesAsync(List<BillItemDto> items)
        {
            // ── BƯỚC 1: Load từ điển vào MemoryCache ──────────────────────────────────
            var dictionary = await _cache.GetOrCreateAsync(DictionaryCacheKey, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = CacheExpiry;
                return await _unitOfWork.ItemDictionaryRepository.GetAllDictionaryAsync();
            }) ?? new List<ItemDictionary>();

            // ── BƯỚC 2: Quét qua từng item, so khớp với từ điển cache ────────────────
            var unresolvedItems = new List<BillItemDto>();

            foreach (var item in items)
            {
                var normalizedName = item.ItemName.Trim().ToLowerInvariant();

                ItemDictionary? bestMatch = null;
                double maxSimilarity = 0.0;

                // TIER 1: So khớp Fuzzy (Tìm kiếm gần đúng) >= 70%
                foreach (var dictItem in dictionary)
                {
                    double similarity = CalculateSimilarity(normalizedName, dictItem.NormalizedKeyword);
                    if (similarity > maxSimilarity)
                    {
                        maxSimilarity = similarity;
                        bestMatch = dictItem;
                    }
                }

                if (maxSimilarity >= 0.70 && bestMatch != null)
                {
                    // ✅ Khớp chuỗi >= 70% → gán luôn
                    item.Category = bestMatch.Category;
                }
                else
                {
                    // ❓ Chưa biết → thêm vào danh sách chờ hỏi LLM
                    unresolvedItems.Add(item);
                }
            }

            // ── BƯỚC 3: Gọi LLM 1 lần duy nhất cho tất cả items chưa biết ────────────
            if (!unresolvedItems.Any()) return;

            var llmResults = await CallLlmToCategorizeBatchAsync(unresolvedItems);
            if (llmResults == null || !llmResults.Any()) return;

            // ── BƯỚC 4: Map kết quả LLM + Tự học (lưu vào DB) ───────────────────────
            var newEntries = new List<ItemDictionary>();

            foreach (var unresolved in unresolvedItems)
            {
                // Tìm kết quả LLM tương ứng theo index
                var llmResult = llmResults.FirstOrDefault(r =>
                    string.Equals(r.ItemName, unresolved.ItemName, StringComparison.OrdinalIgnoreCase));

                var category = llmResult?.Category ?? "Unknown";
                unresolved.Category = category;

                // Chỉ lưu vào từ điển nếu LLM trả về kết quả hợp lệ
                if (category != "Unknown")
                {
                    newEntries.Add(new ItemDictionary
                    {
                        Keyword = unresolved.ItemName,
                        NormalizedKeyword = unresolved.ItemName.Trim().ToLowerInvariant(),
                        Category = category,
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }

            // Batch insert + lưu DB
            if (newEntries.Any())
            {
                await _unitOfWork.ItemDictionaryRepository.AddRangeAsync(newEntries);
                await _unitOfWork.Complete();

                // Invalidate cache để lần sau load lại từ điển đã có data mới
                _cache.Remove(DictionaryCacheKey);
            }
        }

        /// <summary>
        /// Gọi LLM 1 lần duy nhất để phân loại batch items chưa có trong từ điển.
        /// Trả về danh sách item với category được gán bởi LLM.
        /// </summary>
        private async Task<List<LlmCategoryResult>?> CallLlmToCategorizeBatchAsync(List<BillItemDto> unresolvedItems)
        {
            var endpoint = _config["AiModel:Endpoint"] ?? "https://models.inference.ai.azure.com/chat/completions";
            var apiKey = _config["AiModel:ApiKey"] ?? throw new InvalidOperationException("Thiếu API Key của AiModel");
            var modelName = _config["AiModel:ModelName"] ?? "gpt-4o-mini";

            // Chỉ gửi tên món, không cần Price/Quantity để giảm token
            var itemNamesJson = JsonSerializer.Serialize(unresolvedItems.Select(x => x.ItemName).ToList());

            var prompt =
                "Bạn là chuyên gia phân loại hàng hóa. Hãy phân loại các mặt hàng dưới đây theo từng danh mục cụ thể bằng tiếng Việt (ví dụ: Thực phẩm, Đồ nội thất, Đồ điện tử, Quần áo, Văn phòng phẩm, Mỹ phẩm, v.v.). " +
                "Nếu không thể xác định được danh mục của một mặt hàng, BẮT BUỘC để Category là 'Unknown'. Không dùng 'Food/Object' hay 'Object'. " +
                "BẮT BUỘC trả về một JSON object có dạng: { \"items\": [ {\"ItemName\": \"...\", \"Category\": \"Thực phẩm\"} ] }. " +
                "Giữ nguyên tên món (ItemName) y hệt như trong input. Không giải thích thêm." +
                "\n\nDanh sách cần phân loại (JSON array):\n" + itemNamesJson;

            var payload = new
            {
                model = modelName,
                messages = new[] { new { role = "user", content = prompt } },
                response_format = new { type = "json_object" },
                temperature = 0.0
            };

            try
            {
                var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
                request.Headers.Add("Authorization", $"Bearer {apiKey}");
                request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

                var client = _httpClientFactory.CreateClient();
                var response = await client.SendAsync(request);

                if (!response.IsSuccessStatusCode) return null;

                var responseString = await response.Content.ReadAsStringAsync();
                var jsonNode = JsonNode.Parse(responseString);
                var aiTextResponse = jsonNode?["choices"]?[0]?["message"]?["content"]?.ToString();

                if (string.IsNullOrWhiteSpace(aiTextResponse)) return null;

                var resultObj = JsonSerializer.Deserialize<LlmBatchCategoryResponse>(
                    aiTextResponse,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                return resultObj?.Items;
            }
            catch
            {
                // Lỗi LLM không làm hỏng toàn bộ response — category giữ null
                return null;
            }
        }


        /// <summary>
        /// Thuật toán Levenshtein Distance tính số bước biến đổi chuỗi.
        /// </summary>
        private static int CalculateLevenshteinDistance(string source, string target)
        {
            if (string.IsNullOrEmpty(source)) return target?.Length ?? 0;
            if (string.IsNullOrEmpty(target)) return source.Length;

            int[] costs = new int[target.Length + 1];
            for (int i = 0; i <= source.Length; i++)
            {
                int previousValue = i;
                for (int j = 0; j <= target.Length; j++)
                {
                    if (i == 0)
                    {
                        costs[j] = j;
                    }
                    else if (j > 0)
                    {
                        int currentValue = costs[j - 1];
                        if (source[i - 1] != target[j - 1])
                        {
                            currentValue = Math.Min(Math.Min(currentValue, previousValue), costs[j]) + 1;
                        }
                        costs[j - 1] = previousValue;
                        previousValue = currentValue;
                    }
                }
                if (i > 0) costs[target.Length] = previousValue;
            }
            return costs[target.Length];
        }

        /// <summary>
        /// Tính tỷ lệ tương đồng (0.0 đến 1.0) dựa trên Levenshtein Distance.
        /// </summary>
        private static double CalculateSimilarity(string source, string target)
        {
            if (source == target) return 1.0;
            if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(target)) return 0.0;

            int distance = CalculateLevenshteinDistance(source, target);
            int maxLength = Math.Max(source.Length, target.Length);
            
            return 1.0 - ((double)distance / maxLength);
        }


        private class AiAnalyzeResult
        {
            public string ItemName { get; set; } = string.Empty;
            public string Category { get; set; } = string.Empty;
            public int EstimatedCalories { get; set; }
            public long EstimatedPriceVND { get; set; }
        }

        /// <summary>Mỗi item trong kết quả LLM batch trả về.</summary>
        private class LlmCategoryResult
        {
            public string ItemName { get; set; } = string.Empty;
            public string Category { get; set; } = string.Empty;
        }

        /// <summary>Wrapper JSON object LLM trả về: { "items": [...] }</summary>
        private class LlmBatchCategoryResponse
        {
            public List<LlmCategoryResult> Items { get; set; } = new();
        }

        
    }
}
