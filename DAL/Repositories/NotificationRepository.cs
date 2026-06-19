using DAL.Data;
using DAL.Entities;
using DAL.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace DAL.Repositories
{
    public class NotificationRepository(AppDbContext context) : GenericRepository<Notification>(context), INotificationRepository
    {
        public async Task<IEnumerable<Notification>> GetByUserIdAsync(string userId)
        {
            return await _dbSet.Where(x => x.UserId == userId).ToListAsync();
        }
    }
}
