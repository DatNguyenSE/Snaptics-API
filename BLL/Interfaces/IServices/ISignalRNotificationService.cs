using System.Threading.Tasks;
using BLL.Dtos;

namespace BLL.Interfaces.IServices
{
    public interface ISignalRNotificationService
    {
        Task SendNotificationAsync(string userId, NotificationDto notification);
    }
}
