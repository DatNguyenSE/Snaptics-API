using DAL.Entities;

namespace DAL.IRepositories
{
    public interface INotificationRepository : IGenericRepository<Notification>
    {
        Task<IEnumerable<Notification>> GetByUserIdAsync(string userId);
        Task CleanUpOldNotificationsAsync();
    }
}
