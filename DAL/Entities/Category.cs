using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.Domain.Enums;

namespace API.Entities
{
    public class Category
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public CategoryType Type { get; set; } = CategoryType.Expense;
        public bool IsTrackableInventory { get; set; } = false;
    }
}