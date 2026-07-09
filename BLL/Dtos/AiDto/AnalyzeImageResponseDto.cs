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

        public decimal Quantity { get; set; } = 1;

        public int EstimatedCalories { get; set; }

        public long EstimatedPriceVND { get; set; }

        // public string? ImageKey { get; set; } // S3 key của ảnh đã upload

        /// <summary>Đơn vị tính: ly, cái, tô, hộp...</summary>
        public string Unit { get; set; } = string.Empty;
    }
}
