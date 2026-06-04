using System;
using System.Collections.Generic;
using System.Text;

namespace BLL.Dtos
{
    public class TransactionDetailDto
    {
        public int Id { get; set; }
        public int TransactionId { get; set; }
        public int CategoryId { get; set; }
        public String ItemName { get; set; } = null!;
        public decimal Price { get; set; }
        public int Quantity { get; set; }

        public int? EstimatedCalories { get; set; }
    }
}
