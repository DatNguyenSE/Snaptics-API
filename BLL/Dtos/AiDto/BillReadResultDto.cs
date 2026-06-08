namespace BLL.Dtos.AiDto
{
    /// <summary>
    /// Kết quả đọc bill/hóa đơn từ Azure Document Intelligence.
    /// Client dùng data này để confirm rồi gọi POST /Transaction + POST /TransactionDetail để lưu.
    /// </summary>
    public class BillReadResultDto
    {
        public string? MerchantName { get; set; }

        public DateTime? TransactionDate { get; set; }

        public decimal TotalAmount { get; set; }

        public string? Currency { get; set; }

        public List<BillItemDto> Items { get; set; } = new();
    }

    public class BillItemDto
    {
        public string ItemName { get; set; } = string.Empty;

        public decimal Price { get; set; }

        /// <summary>Số lượng hoặc khối lượng (kg) — dùng decimal để hỗ trợ hàng bán theo cân</summary>
        public decimal Quantity { get; set; } = 1;

        public decimal TotalPrice => Price * Quantity;

        /// <summary>Category được gán sau khi phân loại: "Food", "Object", hoặc null nếu chưa xử lý</summary>
        public string? Category { get; set; }
    }
}
