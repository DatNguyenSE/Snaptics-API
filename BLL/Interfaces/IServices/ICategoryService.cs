using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BLL.Dtos;

namespace BLL.Interfaces.IServices
{
    public interface ICategoryService
    {
        Task<IEnumerable<CategoryDto>> GetAllAsync();
        Task<CategoryDto> GetByIdAsync(int categoryId);
        Task<CategoryDto> CreateAsync(CategoryDto categoryDto);
        Task<CategoryDto> CreateByNameAsync(string categoryName);
        Task CreateMissingCategoriesAsync(IEnumerable<string> categoryNames);
    }
}