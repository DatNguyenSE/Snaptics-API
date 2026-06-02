using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace API.Domain.Enums
{
    public enum TransactionStatusType
    {
        Completed,    // Đã hoàn tất
        MissingPrice  // Thiếu giá, chờ AI hoặc User nhập
    }
}