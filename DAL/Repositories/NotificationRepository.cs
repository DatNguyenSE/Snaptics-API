using DAL.Data;
using DAL.Entities;
using DAL.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace DAL.Repositories
{
    public class NotificationRepository(AppDbContext context) : GenericRepository<Notification>(context), INotificationRepository
    {
        public async Task CleanUpOldNotificationsAsync()
        {
            var readNotifications = await _dbSet
                .Where(n => n.IsRead == true)
                .OrderByDescending(n => n.CreatedAt) 
                .ToListAsync();

            var notificationsToDelete = readNotifications.Skip(10).ToList();
            
            if (notificationsToDelete.Any())
                {
                    _dbSet.RemoveRange(notificationsToDelete);
                }
        }

        public async Task<IEnumerable<Notification>> GetByUserIdAsync(string userId)
        {
            return await _dbSet.Where(x => x.UserId == userId).ToListAsync();
        }
        
    }
}
