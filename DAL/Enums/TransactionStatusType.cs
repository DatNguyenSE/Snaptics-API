namespace DAL.Enums
{
    public enum TransactionStatusType
    {
        Completed,    // Đã hoàn tất
        MissingPrice  // Thiếu giá, chờ AI hoặc User nhập
    }
}