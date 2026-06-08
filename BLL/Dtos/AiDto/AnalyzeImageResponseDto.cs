namespace BLL.Dtos.AiDto
{
    /// <summary>
    /// Kết quả phân tích ảnh từ Gemini Vision LLM.
    /// Client dùng data này để tự gọi POST /TransactionDetail để lưu.
    /// </summary>
    public class AnalyzeImageResponseDto
    {
        public string ItemName { get; set; } = string.Empty;

        /// <summary>"Food" hoặc "Object"</summary>
        public string Category { get; set; } = string.Empty;

        public int EstimatedCalories { get; set; }

        public long EstimatedPriceVND { get; set; }
    }
}
