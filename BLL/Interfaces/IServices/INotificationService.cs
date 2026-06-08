using BLL.Dtos;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BLL.Interfaces.IServices
{
    public interface INotificationService
    {
        Task<IEnumerable<NotificationDto>> GetAllAsync();
        Task<NotificationDto> GetByIdAsync(int id);
        Task<NotificationDto> CreateAsync(NotificationDto notificationDto);
        Task<NotificationDto> UpdateAsync(int id, NotificationDto notificationDto);
        Task<NotificationDto> DeleteAsync(int id);
    }
}
