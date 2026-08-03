using System.Threading.Tasks;

namespace BLL.Interfaces.IServices
{
    public interface IMonthlyAiInsightJobService
    {
        Task GenerateMonthlyInsightsAsync();
    }
}
