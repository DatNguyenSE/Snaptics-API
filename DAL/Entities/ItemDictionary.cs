using System;

namespace DAL.Entities
{
    /// <summary>
    /// Bảng từ điển nội bộ lưu các mặt hàng đã được phân loại.
    /// Dùng để tra cứu nhanh (cache) trước khi gọi LLM, giảm chi phí API.
    /// </summary>
    public class ItemDictionary
    {
        public int Id { get; set; }

        /// <summary>Tên gốc của item (ví dụ: "Sữa chua Vinamilk 100ml")</summary>
        public string Keyword { get; set; } = string.Empty;

        /// <summary>Keyword đã chuẩn hóa (lowercase, trim) — dùng để so sánh tốc độ cao</summary>
        public string NormalizedKeyword { get; set; } = string.Empty;

        /// <summary>"Food" hoặc "Object"</summary>
        public string Category { get; set; } = string.Empty;

        /// <summary>Số lần mặt hàng được so khớp thành công</summary>
        public int HitCount { get; set; } = 1;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
