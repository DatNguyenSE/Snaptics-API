using System;

namespace BLL.Dtos
{
    public class ItemDictionaryDto
    {
        public int Id { get; set; }
        public required string Keyword { get; set; }
        public string NormalizedKeyword { get; set; } = string.Empty;
        public required string Category { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
