using System;
using System.Collections.Generic;
using System.Text;

namespace BLL.Dtos.AiAssistantDto
{
    public class CategorySpendingDto
    {
        public string CategoryName { get; set; } = string.Empty;

        public decimal TotalAmount { get; set; }
    }
}
