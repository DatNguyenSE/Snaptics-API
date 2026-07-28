using System.Collections.Generic;
using BLL.Dtos;

namespace BLL.Dtos.Admin
{
    public class AdminUserStatsDto
    {
        public int TotalUsers { get; set; }
        public int ActiveUsers { get; set; }
        public int LockedUsers { get; set; }
        public int UnverifiedUsers { get; set; }
    }

    public class AdminUserResponseDto
    {
        public AdminUserStatsDto Stats { get; set; } = new AdminUserStatsDto();
        public PaginatedResultDto<AdminUserDto> Users { get; set; } = new PaginatedResultDto<AdminUserDto>();
    }
}
