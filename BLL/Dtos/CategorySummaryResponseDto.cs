using System;
using System.Collections.Generic;

namespace BLL.Dtos
{
    public class CategorySummaryResponseDto
    {
        public CategorySummaryItemDto TopCategory { get; set; }
        public List<CategorySummaryItemDto> Breakdown { get; set; } = new();
    }

    public class CategorySummaryItemDto
    {
        public string Name { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal Percentage { get; set; }
    }
}
