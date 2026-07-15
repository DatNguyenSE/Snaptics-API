using System;
using System.Collections.Generic;
using System.Text;

namespace BLL.Interfaces.IServices
{
    public interface IIncomeJobService
    {
        Task ProcessRecurringIncomeAsync();
    }
}
