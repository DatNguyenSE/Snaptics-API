using System.Threading.Tasks;
using BLL.Dtos;

namespace BLL.Interfaces.IServices
{
    public interface IMaintenanceService
    {
        MaintenanceConfigDto GetConfig();
        void UpdateConfig(MaintenanceConfigDto config);
        bool IsMaintenanceMode();
    }
}
