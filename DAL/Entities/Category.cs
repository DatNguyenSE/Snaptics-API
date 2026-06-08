using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DAL.Enums;

namespace DAL.Entities
{
    public class Category
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public CategoryType Type { get; set; } = CategoryType.Expense;
        public bool IsTrackableInventory { get; set; } = false;
        public string? Icon { get; set; } 
        public string? Color { get; set; } 
        public string? Status { get; set; } = "Active";

    }
}