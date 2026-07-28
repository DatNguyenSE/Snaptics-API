using System;
using System.Collections.Generic;

namespace BLL.Dtos.Admin
{
    public class AdminUserDto
    {
        public string Id { get; set; } = null!;
        public string? Email { get; set; }
        public string? DisplayName { get; set; }
        public string? Status { get; set; }
        public bool IsEmailConfirmed { get; set; }
        public bool IsLockedOut { get; set; }
        public IList<string> Roles { get; set; } = new List<string>();
        public DateTime CreatedAt { get; set; }
    }
}
