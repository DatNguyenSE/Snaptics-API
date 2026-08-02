using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BLL.Dtos;

namespace BLL.Interfaces.IServices
{
    public interface ICategoryService
    {
        Task<IEnumerable<CategoryDto>> GetAllAsync(string? userId = null);
        Task<bool> SetInventoryTrackingAsync(int categoryId, string userId, bool isTrackableInventory);
        Task<bool> GetEffectiveInventoryTrackingAsync(int categoryId, string userId);
        Task<CategoryDto> GetByIdAsync(int categoryId);
        Task<CategoryDto> CreateAsync(CategoryDto categoryDto);
        Task<CategoryDto> CreateByNameAsync(string categoryName, string userId);
        Task<CategoryDto> UpdateAsync(int categoryId, CategoryDto categoryDto, string? userId = null);
        Task<CategoryDto> DeleteAsync(int categoryId, string? userId = null);
        Task CreateMissingCategoriesAsync(IEnumerable<string> categoryNames);
    }
}
