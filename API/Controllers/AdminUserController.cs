using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BLL.Dtos;
using BLL.Dtos.Admin;
using DAL.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers
{
    [ApiController]
    [Route("api/admin/users")]
    [Authorize(Roles = "Admin")]
    public class AdminUserController : ControllerBase
    {
        private readonly UserManager<AppUser> _userManager;

        public AdminUserController(UserManager<AppUser> userManager)
        {
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> GetUsers([FromQuery] AdminUserQueryDto query)
        {
            var dbQuery = _userManager.Users.AsNoTracking();

            // Calculate overall stats before applying filters
            var totalUsers = await dbQuery.CountAsync();
            var activeUsers = await dbQuery.CountAsync(u => u.Status == "Active" && (u.LockoutEnd == null || u.LockoutEnd <= DateTimeOffset.UtcNow));
            var lockedUsers = await dbQuery.CountAsync(u => u.LockoutEnd != null && u.LockoutEnd > DateTimeOffset.UtcNow);
            var unverifiedUsers = await dbQuery.CountAsync(u => !u.EmailConfirmed);

            // Apply filters
            if (!string.IsNullOrEmpty(query.Search))
            {
                var lowerSearch = query.Search.ToLower();
                dbQuery = dbQuery.Where(u => (u.DisplayName != null && u.DisplayName.ToLower().Contains(lowerSearch)) || 
                                             (u.Email != null && u.Email.ToLower().Contains(lowerSearch)));
            }

            if (!string.IsNullOrEmpty(query.Status))
            {
                if (query.Status.Equals("Active", StringComparison.OrdinalIgnoreCase))
                {
                    dbQuery = dbQuery.Where(u => u.Status == "Active" && (u.LockoutEnd == null || u.LockoutEnd <= DateTimeOffset.UtcNow));
                }
                else if (query.Status.Equals("Locked", StringComparison.OrdinalIgnoreCase))
                {
                    dbQuery = dbQuery.Where(u => u.LockoutEnd != null && u.LockoutEnd > DateTimeOffset.UtcNow);
                }
            }

            if (query.IsEmailConfirmed.HasValue)
            {
                dbQuery = dbQuery.Where(u => u.EmailConfirmed == query.IsEmailConfirmed.Value);
            }

            if (!string.IsNullOrEmpty(query.Role))
            {
                var usersInRole = await _userManager.GetUsersInRoleAsync(query.Role);
                var userIdsInRole = usersInRole.Select(u => u.Id).ToList();

                dbQuery = dbQuery.Where(u => userIdsInRole.Contains(u.Id));
            }

            var filteredTotal = await dbQuery.CountAsync();
            
            var pagedUsers = await dbQuery
                .OrderByDescending(u => u.CreatedAt)
                .Skip((query.Page - 1) * query.Size)
                .Take(query.Size)
                .ToListAsync();

            var mappedUsers = new List<AdminUserDto>();
            foreach (var user in pagedUsers)
            {
                var roles = await _userManager.GetRolesAsync(user);
                mappedUsers.Add(new AdminUserDto
                {
                    Id = user.Id,
                    Email = user.Email,
                    DisplayName = user.DisplayName,
                    Status = user.LockoutEnd != null && user.LockoutEnd > DateTimeOffset.UtcNow ? "Locked" : user.Status,
                    IsEmailConfirmed = user.EmailConfirmed,
                    IsLockedOut = user.LockoutEnd != null && user.LockoutEnd > DateTimeOffset.UtcNow,
                    Roles = roles,
                    CreatedAt = user.CreatedAt
                });
            }

            var response = new AdminUserResponseDto
            {
                Stats = new AdminUserStatsDto
                {
                    TotalUsers = totalUsers,
                    ActiveUsers = activeUsers,
                    LockedUsers = lockedUsers,
                    UnverifiedUsers = unverifiedUsers
                },
                Users = new PaginatedResultDto<AdminUserDto>
                {
                    Items = mappedUsers,
                    TotalCount = filteredTotal,
                    Page = query.Page,
                    Size = query.Size
                }
            };

            return Ok(response);
        }
    }
}
