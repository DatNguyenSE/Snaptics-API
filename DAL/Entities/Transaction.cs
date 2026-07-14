using DAL.Enums;


namespace DAL.Entities
{
    public class Transaction
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? UserId { get; set; }

        public string? ImageKey { get; set; }

        public decimal TotalAmount { get; set; } = 0m;

        public DateTime TransactionDate { get; set; } = DateTime.UtcNow;

        public TransactionStatusType Status { get; set; } = TransactionStatusType.Completed;

        public bool IsAiEstimated { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? Note { get; set; }
        public bool IsDeleted { get; set; } = false;
        public bool IsExpense { get; set; } = true;

        public int? BudgetId { get; set; }
        public virtual Budget Budget { get; set; }

        // Navigation Properties
        public virtual AppUser AppUsers { get; set; }
        public virtual ICollection<TransactionDetail> TransactionDetails { get; set; } = new List<TransactionDetail>();
    }
}