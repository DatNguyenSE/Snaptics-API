using BLL.Dtos;
using System;
using System.Collections.Generic;
using System.Text;

namespace BLL.Interfaces.IServices
{
    public interface IIncomeSourceService
    {
        Task<IEnumerable<IncomeSourceDto>> GetAllAsync();

        Task<IncomeSourceDto> GetByIdAsync(int id);

        Task<IEnumerable<IncomeSourceDto>> GetByUserIdAsync(string userId);

        Task<IncomeSourceDto> CreateAsync(IncomeSourceDto dto);

        Task<IncomeSourceDto> UpdateAsync(int id, IncomeSourceDto dto);

        Task<IncomeSourceDto> DeleteAsync(int id);
    }
}
