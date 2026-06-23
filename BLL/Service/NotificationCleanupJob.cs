using DAL.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace BLL.Interfaces.IServices
{
    public interface INotificationCleanupJob
    {
        Task CleanUpOldNotificationsAsync();
    }
}

namespace BLL.Service
{
    public class NotificationCleanupJob(AppDbContext _context) : Interfaces.IServices.INotificationCleanupJob
    {
        public async Task CleanUpOldNotificationsAsync()
        {
            var userIds = await _context.Notifications
                                        .Select(n => n.UserId) 
                                        .Distinct()
                                        .ToListAsync();

            foreach (var userId in userIds)
            {
                var oldNotifications = await _context.Notifications
                    .Where(n => n.UserId == userId)
                    .OrderByDescending(n => n.Id) 
                    .Skip(10) 
                    .ToListAsync();

                if (oldNotifications.Any())
                {
                    _context.Notifications.RemoveRange(oldNotifications);
                }
            }

            await _context.SaveChangesAsync();
        }
    }
}