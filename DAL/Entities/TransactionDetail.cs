using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


namespace DAL.Entities
{
    public class TransactionDetail
    {
        public int Id { get; set; }
        
        public int TransactionId { get; set; }
        
        public int CategoryId { get; set; }
        
        public string ItemName { get; set; }
        
        public decimal Price { get; set; } = 0m;
        
        public decimal Quantity { get; set; } = 1;
        
        public int? EstimatedCalories { get; set; }

        // Navigation Properties
        public virtual Transaction Transaction { get; set; }
        public virtual Category Category { get; set; }
        
        // Mối quan hệ 1-1 với ItemInventory
        public virtual ItemInventory? ItemInventory { get; set; }
    }
}