using BLL.Interfaces.IServices;
using DAL.Entities;
using Microsoft.AspNetCore.Identity;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace BLL.Service
{
    public class MonthlyAiInsightJobService(UserManager<AppUser> _userManager, IAiInsightService _aiInsightService) : IMonthlyAiInsightJobService
    {
        public async Task GenerateMonthlyInsightsAsync()
        {
            var users = await _userManager.Users.ToListAsync();
            foreach (var user in users)
            {
                // Gọi các phân tích tài chính chung
                await _aiInsightService.GenerateInsightsAsync(user.Id);
            }
        }
    }
}
