namespace DAL.Entities
{
    public class UserCategorySetting
    {
        public int Id { get; set; }
        public string UserId { get; set; } = null!;
        public int CategoryId { get; set; }
        public bool IsTrackableInventory { get; set; }

        public AppUser User { get; set; } = null!;
        public Category Category { get; set; } = null!;
    }
}
