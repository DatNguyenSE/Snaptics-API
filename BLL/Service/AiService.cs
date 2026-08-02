using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Azure;
using Azure.AI.FormRecognizer.DocumentAnalysis;
using BLL.Dtos.AiDto;
using BLL.Helpers;
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

#region Fields, Prompt and Constructor
        
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
            "Đóng vai một chuyên gia thẩm định giá. " +
            "Hãy phân tích hình ảnh này để xác định đây là món đồ hay thực phẩm gì. " +
            "CHÚ Ý CỰC KỲ QUAN TRỌNG: BẠN PHẢI QUAN SÁT KỸ VÀ ĐẾM CHÍNH XÁC SỐ LƯỢNG VẬT THỂ CÙNG LOẠI XUẤT HIỆN TRONG ẢNH. " +
            "Ví dụ 1: Nếu thấy 2 ly trà sữa, itemName là 'Trà sữa (2 ly)', quantity là 2, category là 'Drink', estimatedPriceVND là tổng giá 2 ly. " +
            "Ví dụ 2: Nếu thấy 1 cái ghế, itemName là 'Ghế văn phòng', quantity là 1, unit là 'cái', category là 'Furniture', estimatedPriceVND là giá thị trường ước tính của 1 cái ghế đó. " +
            "Quy tắc phân loại (category): Phân loại càng cụ thể càng tốt bằng TIẾNG ANH (ví dụ: Food, Drink, Furniture, Electronics, Clothing, Stationery, Cosmetics, Necessities, v.v.). 'Drink' dành riêng cho thức uống. Nếu không thể xác định, BẮT BUỘC để category là 'Unknown'. Không trả về chuỗi chung chung như 'Food/Object'. " +
            "Trả về cho tôi một chuỗi JSON chuẩn có cấu trúc: " +
            "{ \"itemName\": \"\", \"quantity\": <điền số lượng đếm được>, \"unit\": \"<đơn vị tính: ly, cái, hộp, phần, chai...>\", \"category\": \"\", \"estimatedPriceVND\": 0 }. " +
            "Chỉ trả về JSON, không giải thích gì thêm. Không bọc kết quả trong markdown (như ```json ).";

        public AiService(IHttpClientFactory httpClientFactory, IConfiguration config,
            IUnitOfWork unitOfWork, IMemoryCache cache)
        {
            _httpClientFactory = httpClientFactory;
            _config = config;
            _unitOfWork = unitOfWork;
            _cache = cache;
        }
#endregion
        
        // ═══════════════════════════════════════════════════════
        // Feature 1: Phân tích ảnh bằng AI (ChatGPT / gpt-4o-mini)
        public async Task<AnalyzeImageResponseDto> AnalyzeImageAsync(byte[] imageBytes, string contentType, string userId, bool estimatePrice = true)
        {
            var endpoint = _config["AiModel:Endpoint"] ?? "https://models.inference.ai.azure.com/chat/completions";
            var apiKey = _config["AiModel:ApiKey"] ?? throw new InvalidOperationException("Thiếu API Key của AiModel");
            var modelName = _config["AiModel:ModelName"] ?? "gpt-4o-mini";

            // 1. Kiểm tra tính hợp lệ của file ảnh đầu vào (không rỗng)
            if (imageBytes == null || imageBytes.Length == 0)
                throw new ArgumentException("File ảnh không hợp lệ hoặc rỗng.");

            var allowedTypes = new[] { "image/jpeg", "image/png", "image/webp", "image/heic" };
            if (!allowedTypes.Contains(contentType.ToLower()))
                throw new ArgumentException($"Định dạng ảnh không hỗ trợ: {contentType}. Hỗ trợ: jpg, png, webp, heic.");

            // Lấy danh sách category từ DB
            var categories = await _unitOfWork.CategoryRepository.FindAsync(c => !c.IsDeleted && (c.IsDefault || c.UserId == userId));
            var userCategoryNames = categories
                .Where(c => c.UserId == userId && !string.IsNullOrWhiteSpace(c.Name))
                .Select(c => c.Name!.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var userCategoryNameSet = userCategoryNames.ToHashSet(StringComparer.OrdinalIgnoreCase);
            var systemCategoryNames = categories
                .Where(c => c.IsDefault && !string.IsNullOrWhiteSpace(c.Name))
                .Select(c => c.Name!.Trim())
                .Where(name => !userCategoryNameSet.Contains(name))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var userCategoryListStr = userCategoryNames.Any()
                ? string.Join(", ", userCategoryNames)
                : "(chưa có danh mục riêng)";
            var systemCategoryListStr = systemCategoryNames.Any()
                ? string.Join(", ", systemCategoryNames)
                : "(không có danh mục hệ thống bổ sung)";

            // 2. Chuyển đổi sang chuỗi Base64
            string base64Image = Convert.ToBase64String(imageBytes);

            // 3. Xây dựng Prompt động dựa trên cấu hình người dùng (Nâng cao độ chính xác)
            var promptBuilder = new StringBuilder();
            promptBuilder.Append("Bạn là một chuyên gia AI phân tích hình ảnh chuyên sâu");
            if (estimatePrice) promptBuilder.Append(" kết hợp vai trò chuyên gia thẩm định giá");
            promptBuilder.Append(". Hãy thực hiện phân tích bức ảnh này và trả về kết quả đáp ứng các tiêu chí nghiêm ngặt sau:\n");

            promptBuilder.Append("1. TÊN VẬT THỂ (itemName):\n");
            promptBuilder.Append("   - BẮT BUỘC viết bằng TIẾNG VIỆT có dấu.\n");
            promptBuilder.Append("   - Hãy quan sát kỹ để lấy tên chi tiết nhất có thể. KHÔNG bị giới hạn chỉ đồ ăn, bạn có thể nhận diện TẤT CẢ mọi thứ từ Đồ chơi, Đồ điện tử, Nội thất, Đồ gia dụng, Quần áo, Xe cộ, Văn phòng phẩm, v.v.\n");
            promptBuilder.Append("   - MẸO QUAN TRỌNG VỚI ĐỒ ĂN: Nếu trong ảnh là một mâm đồ ăn hoặc một món ăn có nhiều thành phần (ví dụ: mẹt gồm đậu phụ, thịt luộc, chả cốm...), hãy NHẬN DIỆN TÊN TỔNG THỂ CỦA MÓN ĐÓ (ví dụ: 'Bún đậu mắm tôm thập cẩm') thay vì chỉ liệt kê lẻ tẻ một nguyên liệu.\n");
            promptBuilder.Append("   - KHÔNG gộp số lượng và đơn vị vào tên vật thể (itemName).\n\n");

            promptBuilder.Append("2. ĐƠN VỊ TÍNH VÀ SỐ LƯỢNG:\n");
            promptBuilder.Append("   - Đếm chính xác số lượng (quantity) của vật thể.\n");
            promptBuilder.Append("   - BẮT BUỘC cung cấp đơn vị tính (unit) bằng tiếng Việt (ví dụ: 'cái', 'chiếc', 'hộp', 'ly', 'quyển', 'bộ', v.v.).\n\n");

            promptBuilder.Append("3. DANH MỤC (category):\n");
            promptBuilder.Append($"   - DANH MỤC RIÊNG CỦA USER (ưu tiên số 1): [{userCategoryListStr}].\n");
            promptBuilder.Append($"   - DANH MỤC HỆ THỐNG (ưu tiên số 2): [{systemCategoryListStr}].\n");
            promptBuilder.Append("   - Nếu danh mục riêng của USER trùng hoặc tương đương với danh mục hệ thống, BẮT BUỘC ưu tiên danh mục riêng của USER.\n");
            promptBuilder.Append("   - Hãy ưu tiên lựa chọn một danh mục phù hợp nhất từ danh sách trên nếu có thể.\n");
            promptBuilder.Append("   - BẮT BUỘC trả về TÊN CHÍNH XÁC của danh mục như trong danh sách.\n");
            promptBuilder.Append("   - Nếu vật thể KHÔNG THUỘC nhóm ý nghĩa của bất kỳ danh mục nào (ví dụ: đồ chơi không thể xếp vào đồ ăn), hãy mạnh dạn trả về chữ 'Khác'.\n");
            promptBuilder.Append("   - TUYỆT ĐỐI KHÔNG ĐƯỢC TỰ SÁNG TẠO THÊM TÊN DANH MỤC MỚI.\n\n");

            if (estimatePrice)
            {
                promptBuilder.Append("4. ƯỚC TÍNH GIÁ (estimatedPriceVND):\n");
                promptBuilder.Append("   - Ước tính tổng giá trị thị trường của (các) vật thể đó bằng Việt Nam Đồng (VND).\n");
                promptBuilder.Append("   - Giá phải nhân tương ứng với số lượng đếm được.\n\n");
            }

            promptBuilder.Append("Trả về duy nhất một chuỗi JSON chuẩn có cấu trúc: {\n");
            promptBuilder.Append("  \"itemName\": \"\",\n");
            promptBuilder.Append("  \"quantity\": <số lượng vật thể đếm được (decimal/int)>,\n");
            promptBuilder.Append("  \"unit\": \"<đơn vị tính>\",\n");
            promptBuilder.Append("  \"category\": \"\"");
            if (estimatePrice) promptBuilder.Append(",\n  \"estimatedPriceVND\": <tổng giá trị ước tính VND (long)>");
            promptBuilder.Append("\n}. Chỉ trả về JSON, không kèm giải thích hay bất kỳ chữ nào khác. Không bọc kết quả trong markdown (```json).");

            var dynamicPrompt = promptBuilder.ToString();

            // 4. Xây dựng Payload theo chuẩn OpenAI API.
            // Payload bao gồm Prompt và ảnh dạng Base64.
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
                            new { type = "text", text = dynamicPrompt },
                            new 
                            { 
                                type = "image_url", 
                                image_url = new { url = $"data:{contentType};base64,{base64Image}" } 
                            }
                        }
                    }
                },
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
                    Quantity = result.Quantity,
                    EstimatedCalories = result.EstimatedCalories,
                    EstimatedPriceVND = result.EstimatedPriceVND,
                    Unit = result.Unit
                };
            }
            catch (JsonException ex)
            {
                throw new Exception($"Lỗi Parse JSON: {ex.Message}. Chuỗi AI: {aiTextResponse}");
            }
        }

        // ═══════════════════════════════════════════════════════
        // Feature 2: Đọc Bill bằng Azure Document Intelligence

        public async Task<BillReadResultDto> ReadBillAsync(byte[] imageBytes, string contentType)
        {
            // 1. Kiểm tra tính hợp lệ của file hóa đơn
            if (imageBytes == null || imageBytes.Length == 0)
                throw new ArgumentException("File bill không hợp lệ hoặc rỗng.");

            var allowedTypes = new[] { "image/jpeg", "image/png", "image/tiff", "application/pdf" };
            if (!allowedTypes.Contains(contentType.ToLower()))
                throw new ArgumentException($"Định dạng không hỗ trợ: {contentType}. Hỗ trợ: jpg, png, tiff, pdf.");

            // 2. Khởi tạo Azure Document Intelligence client với Endpoint và API Key
            var credential = new AzureKeyCredential(AzureKey);
            var docClient = new DocumentAnalysisClient(new Uri(AzureEndpoint), credential);

            // 3. Gửi ảnh lên Azure để phân tích bằng mô hình chuyên dụng cho hóa đơn (prebuilt-receipt)
            AnalyzeDocumentOperation operation;
            using (var stream = new MemoryStream(imageBytes))
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

                // Transaction Date and Time
                if (receipt.Fields.TryGetValue("TransactionDate", out var dateField)
                    && dateField.FieldType == DocumentFieldType.Date)
                {
                    var transactionDate = dateField.Value.AsDate().DateTime;
                    
                    if (receipt.Fields.TryGetValue("TransactionTime", out var timeField)
                        && timeField.FieldType == DocumentFieldType.Time)
                    {
                        var transactionTime = timeField.Value.AsTime();
                        transactionDate = transactionDate.Add(transactionTime);
                    }
                    
                    billResult.TransactionDate = transactionDate;
                }
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
                        {
                            var rawName = descField.Content ?? "Unknown Item";
                            // Loại bỏ ký tự xuống dòng và làm sạch khoảng trắng thừa
                            var cleanName = rawName.Replace("\r\n", " ").Replace("\n", " ").Replace("\r", " ");
                            billItem.ItemName = System.Text.RegularExpressions.Regex.Replace(cleanName, @"\s+", " ").Trim();
                        }

                        // Fix: Dùng decimal và parse tay từ Content để tránh lỗi locale vi-VN (hiểu nhầm 1.344 thành 1344)
                        if (itemDict.TryGetValue("Quantity", out var qtyField))
                        {
                            bool isParsed = false;
                            if (!string.IsNullOrWhiteSpace(qtyField.Content))
                            {
                                // Tìm Unit từ string (vd: "1.344 kg" -> "kg")
                                var textPart = new string(qtyField.Content.Where(char.IsLetter).ToArray()).ToLower();
                                if (!string.IsNullOrWhiteSpace(textPart))
                                {
                                    billItem.Unit = textPart;
                                }

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

                        // Tự động suy luận Unit nếu chưa có hoặc bằng "cái"
                        if (string.IsNullOrWhiteSpace(billItem.Unit) || billItem.Unit == "cái")
                        {
                            if (billItem.Quantity % 1 != 0) // Là số thập phân
                            {
                                billItem.Unit = "kg";
                            }
                            else if (!string.IsNullOrWhiteSpace(billItem.ItemName))
                            {
                                // Thử tìm trong tên
                                var lowerName = billItem.ItemName.ToLower();
                                string[] commonUnits = { "hộp", "chai", "gói", "lốc", "thùng", "cây", "lon", "bình", "cuộn", "bịch", "vỉ", "lọ", "tuýp" };
                                foreach (var u in commonUnits)
                                {
                                    // Kiểm tra xem từ đó có phải là một từ độc lập trong chuỗi không
                                    string pattern = @"\b" + System.Text.RegularExpressions.Regex.Escape(u) + @"\b";
                                    if (System.Text.RegularExpressions.Regex.IsMatch(lowerName, pattern))
                                    {
                                        billItem.Unit = u;
                                        break;
                                    }
                                }
                            }
                        }

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
                
                // Cập nhật lại tổng tiền nếu có các item được đọc ra
                var calculatedTotal = billResult.Items.Sum(i => i.Price * i.Quantity);
                if (calculatedTotal > 0)
                {
                    billResult.TotalAmount = calculatedTotal;
                }
            }

            return billResult;
        }

   
        // Phân loại items bằng pattern: Local Dictionary Cache → LLM Fallback → Self-learning.

        private async Task EnrichItemCategoriesAsync(List<BillItemDto> items)
        {
            // ── BƯỚC 1: Load từ điển vào MemoryCache & Sắp xếp theo độ dài giảm dần ────────
            var dictionary = await _cache.GetOrCreateAsync(DictionaryCacheKey, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = CacheExpiry;
                return await _unitOfWork.ItemDictionaryRepository.GetAllDictionaryAsync();
            }) ?? new List<ItemDictionary>();

            // Ưu tiên khớp từ khóa dài trước (ví dụ: "bắp bò" trước "bò")
            var orderedDictionary = dictionary.OrderByDescending(d => d.NormalizedKeyword.Length).ToList();

            var unresolvedItems = new List<BillItemDto>();
            var matchedIds = new HashSet<int>();

            // ── BƯỚC 2: Quét qua từng item, chuẩn hóa & so khớp nguyên từ hoặc khớp mờ với từ điển cache ─────
            foreach (var item in items)
            {
                var normalizedName = TextNormalizationHelper.NormalizeItemName(item.ItemName);
                if (string.IsNullOrWhiteSpace(normalizedName)) continue;

                ItemDictionary? bestMatch = null;

                // TIER 1: So khớp nguyên từ (Word Boundary)
                foreach (var dictItem in orderedDictionary)
                {
                    if (string.IsNullOrWhiteSpace(dictItem.NormalizedKeyword)) continue;

                    string pattern = @"\b" + System.Text.RegularExpressions.Regex.Escape(dictItem.NormalizedKeyword) + @"\b";
                    if (System.Text.RegularExpressions.Regex.IsMatch(normalizedName, pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                    {
                        bestMatch = dictItem;
                        break; // Khớp từ dài nhất và dừng lại luôn
                    }
                }

                // TIER 2: So khớp mờ (Fuzzy Match) sử dụng Levenshtein nếu Tier 1 thất bại
                if (bestMatch == null)
                {
                    double maxSimilarity = 0.0;
                    ItemDictionary? bestFuzzyMatch = null;

                    foreach (var dictItem in orderedDictionary)
                    {
                        if (string.IsNullOrWhiteSpace(dictItem.NormalizedKeyword)) continue;

                        double similarity = CalculateSimilarity(normalizedName, dictItem.NormalizedKeyword);

                        // Ngưỡng chấp nhận (ví dụ >= 80% tương đồng)
                        if (similarity >= 0.8 && similarity > maxSimilarity)
                        {
                            maxSimilarity = similarity;
                            bestFuzzyMatch = dictItem;
                        }
                    }

                    if (bestFuzzyMatch != null)
                    {
                        bestMatch = bestFuzzyMatch;
                    }
                }

                if (bestMatch != null)
                {
                    // Khớp từ khóa gốc thành công (hoặc khớp mờ thành công) → gán danh mục trực tiếp và lưu lại ID để tăng HitCount
                    item.Category = bestMatch.Category;
                    matchedIds.Add(bestMatch.Id);
                }
                else
                {
                    // Chưa khớp từ khóa nào → Cần gọi LLM trực tiếp để phân loại và trích xuất RootKeyword
                    unresolvedItems.Add(item);
                }
            }

            bool isDbChanged = false;

            // ── BƯỚC 3: Cập nhật HitCount cho các từ khóa gốc được so khớp thành công ──────
            if (matchedIds.Any())
            {
                foreach (var matchedId in matchedIds)
                {
                    var dbItem = await _unitOfWork.ItemDictionaryRepository.GetByIdAsync(matchedId);
                    if (dbItem != null)
                    {
                        dbItem.HitCount++;
                        _unitOfWork.ItemDictionaryRepository.Update(dbItem);
                        isDbChanged = true;
                    }
                }
            }

            // ── BƯỚC 4: Gọi LLM cho các Item chưa được phân loại ──────────────────────
            if (unresolvedItems.Any())
            {
                // Loại bỏ các phần tử trùng lặp theo tên chuẩn hóa của item gốc
                var distinctUnresolvedItems = unresolvedItems
                    .GroupBy(x => TextNormalizationHelper.NormalizeItemName(x.ItemName))
                    .Select(g => g.First())
                    .ToList();

                var llmResults = await CallLlmToCategorizeBatchAsync(distinctUnresolvedItems);
                if (llmResults != null && llmResults.Any())
                {
                    var newDictEntries = new List<ItemDictionary>();

                    foreach (var unresolvedItem in distinctUnresolvedItems)
                    {
                        var normalizedName = TextNormalizationHelper.NormalizeItemName(unresolvedItem.ItemName);
                        
                        var llmResult = llmResults.FirstOrDefault(r =>
                            string.Equals(TextNormalizationHelper.NormalizeItemName(r.ItemName), normalizedName, StringComparison.OrdinalIgnoreCase));

                        var category = llmResult?.Category ?? "Unknown";
                        
                        // Lấy RootKeyword do LLM trích xuất, nếu trống thì fallback về tên gốc
                        var rawRootKeyword = string.IsNullOrWhiteSpace(llmResult?.RootKeyword) 
                            ? unresolvedItem.ItemName 
                            : llmResult.RootKeyword;

                        var normalizedRootKeyword = TextNormalizationHelper.NormalizeItemName(rawRootKeyword);
                        if (string.IsNullOrWhiteSpace(normalizedRootKeyword))
                        {
                            normalizedRootKeyword = normalizedName;
                        }

                        // Cập nhật category cho các món trong bill hiện tại
                        foreach (var item in items.Where(i => TextNormalizationHelper.NormalizeItemName(i.ItemName) == normalizedName))
                        {
                            item.Category = category;
                        }

                        // Kiểm tra xem từ khóa gốc này đã tồn tại trong DB hoặc trong danh sách chuẩn bị thêm chưa để tránh chèn trùng
                        bool isAlreadyInDb = dictionary.Any(d => d.NormalizedKeyword == normalizedRootKeyword) ||
                                             newDictEntries.Any(e => e.NormalizedKeyword == normalizedRootKeyword);

                        if (!isAlreadyInDb)
                        {
                            newDictEntries.Add(new ItemDictionary
                            {
                                Keyword = rawRootKeyword,
                                NormalizedKeyword = normalizedRootKeyword,
                                Category = category,
                                HitCount = 1,
                                CreatedAt = DateTime.UtcNow.AddHours(7)
                            });
                        }
                    }

                    if (newDictEntries.Any())
                    {
                        await _unitOfWork.ItemDictionaryRepository.AddRangeAsync(newDictEntries);
                        isDbChanged = true;
                    }
                }
            }

            // ── BƯỚC 5: Lưu thay đổi vào DB & Invalidate Cache ────────────────────────
            if (isDbChanged)
            {
                await _unitOfWork.Complete();
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
                "Bạn là hệ thống AI phân loại hàng hóa siêu thị và hóa đơn. Nhiệm vụ của bạn là đọc tên món hàng (bằng tiếng Việt), gán ĐÚNG 1 danh mục bằng TIẾNG ANH, và trích xuất một TỪ KHÓA GỐC (RootKeyword) đại diện cho loại mặt hàng đó bằng tiếng Việt.\n" +
                "DANH SÁCH DANH MỤC ĐƯỢC PHÉP SỬ DỤNG (CHỈ CHỌN 1 TRONG SỐ NÀY):\n" +
                "- 'Food': Tất cả đồ ăn, thức ăn, gia vị, nông sản, thịt, cá, rau củ...\n" +
                "- 'Drink': Tất cả các loại thức uống (nước ngọt, bia, rượu, trà sữa...)\n" +
                "- 'Necessities': Nhu yếu phẩm, đồ dùng sinh hoạt (xà phòng, giấy vệ sinh, nước giặt...)\n" +
                "- 'Cosmetics': Mỹ phẩm, chăm sóc cơ thể.\n" +
                "- 'Electronics': Đồ điện, điện tử.\n" +
                "- 'Clothing': Quần áo, giày dép.\n" +
                "- 'Stationery': Văn phòng phẩm, sách vở.\n" +
                "- 'Furniture': Đồ nội thất.\n" +
                "- 'Unknown': TUYỆT ĐỐI CHỈ DÙNG khi dòng text là vô nghĩa, hoặc là các khoản phí.\n\n" +
                "QUY TẮC TRÍCH XUẤT TỪ KHÓA GỐC (RootKeyword):\n" +
                "1. RootKeyword phải là danh từ chung cốt lõi mô tả bản chất của sản phẩm, không chứa thương hiệu, không chứa size/dung tích/định lượng hoặc mô tả chi tiết không cần thiết.\n" +
                "2. Ví dụ:\n" +
                "   - 'Lẩu TomYum' -> RootKeyword: 'lẩu'\n" +
                "   - 'Lẩu Nấm Tapinlu' -> RootKeyword: 'lẩu'\n" +
                "   - 'Bắp Bò (nhỏ)' -> RootKeyword: 'bắp bò'\n" +
                "   - 'Gù Bò (nhỏ)' -> RootKeyword: 'gù bò'\n" +
                "   - 'Rau Muống' -> RootKeyword: 'rau'\n" +
                "   - 'Nấm Kim Châm' -> RootKeyword: 'nấm'\n" +
                "   - 'Cá Viên Phô Mai (nhỏ)' -> RootKeyword: 'cá viên'\n" +
                "   - 'Sữa chua Vinamilk 100g' -> RootKeyword: 'sữa chua'\n" +
                "   - 'Nước xốt mè rang Kewpie' -> RootKeyword: 'nước xốt'\n" +
                "   - 'Giày thể thao Adidas' -> RootKeyword: 'giày'\n" +
                "3. Trả về đúng cấu trúc JSON: { \"items\": [ {\"ItemName\": \"<tên gốc>\", \"Category\": \"<1 danh mục tiếng Anh>\", \"RootKeyword\": \"<từ khóa gốc tiếng Việt hạ tông viết thường>\"} ] }\n" +
                "Danh sách mặt hàng đầu vào:\n" + itemNamesJson;

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
            public decimal Quantity { get; set; } = 1;
            public string Unit { get; set; } = string.Empty;
            public int EstimatedCalories { get; set; }
            public long EstimatedPriceVND { get; set; }
        }

        /// <summary>Mỗi item trong kết quả LLM batch trả về.</summary>
        private class LlmCategoryResult
        {
            public string ItemName { get; set; } = string.Empty;
            public string Category { get; set; } = string.Empty;
            public string RootKeyword { get; set; } = string.Empty;
        }

        /// <summary>Wrapper JSON object LLM trả về: { "items": [...] }</summary>
        private class LlmBatchCategoryResponse
        {
            public List<LlmCategoryResult> Items { get; set; } = new();
        }

        
        // ═══════════════════════════════════════════════════════
        // Feature 3: Generate Text Only (For Insights)
        public async Task<string> GenerateTextAsync(string systemPrompt, string userMessage)
        {
            var endpoint = _config["AiModel:Endpoint"] ?? "https://models.inference.ai.azure.com/chat/completions";
            var apiKey = _config["AiModel:ApiKey"] ?? throw new InvalidOperationException("Thiếu API Key của AiModel");
            var modelName = _config["AiModel:ModelName"] ?? "gpt-4o-mini";

            var payload = new
            {
                model = modelName,
                messages = new object[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = userMessage }
                },
                max_tokens = 200,
                temperature = 0.5
            };

            var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            request.Content = new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json");

            var client = _httpClientFactory.CreateClient();
            var response = await client.SendAsync(request);
            var responseString = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"AI Text Gen Error: {response.StatusCode} - {responseString}");
            }

            var jsonNode = JsonNode.Parse(responseString);
            var message = jsonNode?["choices"]?[0]?["message"]?["content"]?.ToString();
            
            return message ?? string.Empty;
        }
    }
}
