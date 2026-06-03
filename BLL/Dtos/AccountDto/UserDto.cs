using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BLL.Dtos
{
    public class UserDto
    {
        public required string Id { get; set; }
        public required string  Email { get; set; }
        public string? ImageUrl { get; set; }
        public string? DisplayName { get; set; } 
        public required string  Token { get; set; }
    }
}