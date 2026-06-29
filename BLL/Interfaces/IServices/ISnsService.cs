using System;
using System.Collections.Generic;
using System.Text;

namespace BLL.Interfaces.IServices
{
    public interface ISnsService
    {
        Task PublishAsync(string subject, string message);
    }
}
