using DAL.Data;
using DAL.Entities;
using DAL.IRepositories;

namespace DAL.Repositories
{
    public class NotificationRepository(AppDbContext context) : GenericRepository<Notification>(context), INotificationRepository
    {
    }
}
