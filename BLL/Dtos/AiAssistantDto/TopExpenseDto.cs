using System;
using System.Collections.Generic;
using System.Text;

namespace BLL.Dtos.AiAssistantDto
{
    public class TopExpenseDto
    {
        public string ItemName { get; set; } = string.Empty;

        public decimal Amount { get; set; }
    }
}