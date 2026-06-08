using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DAL.Enums;

namespace BLL.Dtos
{
    public class CategoryDto
    {
        public int Id { get; set; }
        public required string Name { get; set; } 
        public CategoryType Type { get; set; } = CategoryType.Expense;
        public bool IsTrackableInventory { get; set; }
        public string? Icon { get; set; } 
        public string? Color { get; set; } 
        public string? Status { get; set; }

    }
}
