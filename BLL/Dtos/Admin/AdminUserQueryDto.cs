using System;
using System.Collections.Generic;

namespace BLL.Dtos.Admin
{
    public class AdminUserQueryDto
    {
        public string? Search { get; set; }
        public string? Status { get; set; }
        public bool? IsEmailConfirmed { get; set; }
        public string? Role { get; set; }
        public int Page { get; set; } = 1;
        public int Size { get; set; } = 10;
    }
}
