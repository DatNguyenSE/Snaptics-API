using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace BLL.Interfaces.IServices
{
    public interface IAiInsightService
    {
        Task GenerateInsightsAsync(string userId);
    }
}
