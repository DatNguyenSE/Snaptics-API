using AutoMapper;
using BLL.Dtos;
using BLL.Interfaces.IServices;
using DAL.Entities;
using DAL.IRepositories;

namespace BLL.Service
{
    public class CategoryService(IUnitOfWork _uow, IMapper mapper) : ICategoryService
    {
        public async Task<IEnumerable<CategoryDto>> GetAllAsync(string? userId = null)
        {
            var cates = await _uow.CategoryRepository.FindAsync(c => !c.IsDeleted && (c.IsDefault || (userId != null && c.UserId == userId)));
            var result = mapper.Map<List<CategoryDto>>(cates);

            if (!string.IsNullOrEmpty(userId))
            {
                var settings = await _uow.UserCategorySettingRepository
                    .FindAsync(s => s.UserId == userId);
                var overrides = settings.ToDictionary(s => s.CategoryId, s => s.IsTrackableInventory);

                foreach (var category in result)
                {
                    if (overrides.TryGetValue(category.Id, out var isTrackable))
                    {
                        category.IsTrackableInventory = isTrackable;
                    }
                }
            }

            return result;
        }

        public async Task<bool> GetEffectiveInventoryTrackingAsync(int categoryId, string userId)
        {
            var category = await _uow.CategoryRepository.GetByIdAsync(categoryId);
            if (category == null || category.IsDeleted)
            {
                return false;
            }

            var setting = (await _uow.UserCategorySettingRepository
                .FindAsync(s => s.UserId == userId && s.CategoryId == categoryId))
                .FirstOrDefault();

            return setting?.IsTrackableInventory ?? category.IsTrackableInventory;
        }

        public async Task<bool> SetInventoryTrackingAsync(int categoryId, string userId, bool isTrackableInventory)
        {
            var category = await _uow.CategoryRepository.GetByIdAsync(categoryId);
            if (category == null || category.IsDeleted)
            {
                throw new KeyNotFoundException("Category not found");
            }

            if (!category.IsDefault && category.UserId != userId)
            {
                throw new UnauthorizedAccessException("You cannot update this category");
            }

            // User-owned categories already have their own inventory setting.
            if (!category.IsDefault)
            {
                category.IsTrackableInventory = isTrackableInventory;
                _uow.CategoryRepository.Update(category);
                await _uow.Complete();
                return isTrackableInventory;
            }

            var setting = (await _uow.UserCategorySettingRepository
                .FindAsync(s => s.UserId == userId && s.CategoryId == categoryId))
                .FirstOrDefault();

            if (setting == null)
            {
                setting = new UserCategorySetting
                {
                    UserId = userId,
                    CategoryId = categoryId,
                    IsTrackableInventory = isTrackableInventory
                };
                await _uow.UserCategorySettingRepository.AddAsync(setting);
            }
            else
            {
                setting.IsTrackableInventory = isTrackableInventory;
                _uow.UserCategorySettingRepository.Update(setting);
            }

            await _uow.Complete();
            return isTrackableInventory;
        }

        public async Task<CategoryDto> GetByIdAsync(int categoryId)
        {
            var cate = await _uow.CategoryRepository.GetByIdAsync(categoryId);
            return mapper.Map<CategoryDto>(cate);
        }

        public async Task<CategoryDto> CreateAsync(CategoryDto categoryDto)
        {
            // If UserId is provided, check duplicates only for that user and default categories
            var isDuplicate = false;
            if (string.IsNullOrEmpty(categoryDto.UserId))
            {
                isDuplicate = await _uow.CategoryRepository.AnyAsync(c => c.Name == categoryDto.Name && c.IsDefault && !c.IsDeleted);
            }
            else
            {
                isDuplicate = await _uow.CategoryRepository.AnyAsync(c => c.Name == categoryDto.Name && !c.IsDeleted && (c.IsDefault || c.UserId == categoryDto.UserId));
            }

            if (isDuplicate)
            {
                throw new InvalidOperationException("Category already exists");
            }

            // Map lại từ dto sang entity để lưu vào database
            var entity = mapper.Map<Category>(categoryDto);
            if (!string.IsNullOrEmpty(categoryDto.UserId))
            {
                entity.IsDefault = false;
            }
            
            await _uow.CategoryRepository.AddAsync(entity);
           
            await _uow.Complete();
            return mapper.Map<CategoryDto>(entity);
        }

        public async Task<CategoryDto> CreateByNameAsync(string categoryName, string userId)
        {
            var isDuplicate = await _uow.CategoryRepository.AnyAsync(c => !c.IsDeleted &&
                (c.IsDefault || c.UserId == userId) && c.Name == categoryName);
            if (isDuplicate)
            {
                throw new InvalidOperationException("Category already exists");
            }

            var entity = new Category
            {
                Name = categoryName,
                UserId = userId,
                IsDefault = false
            };
            await _uow.CategoryRepository.AddAsync(entity);
            await _uow.Complete();
            return mapper.Map<CategoryDto>(entity);
        }

        public async Task<CategoryDto> UpdateAsync(int categoryId, CategoryDto categoryDto, string? userId = null)
        {
            ArgumentNullException.ThrowIfNull(categoryDto);

            var entity = await _uow.CategoryRepository.GetByIdAsync(categoryId);
            if (entity == null || entity.IsDeleted)
            {
                throw new KeyNotFoundException("Category not found");
            }

            if (entity.IsDefault)
            {
                throw new UnauthorizedAccessException("System categories cannot be updated directly. Use the user category setting endpoint.");
            }

            if (!entity.IsDefault && entity.UserId != userId)
            {
                throw new UnauthorizedAccessException("You cannot update this category");
            }

            entity.Name = categoryDto.Name;
            entity.IsTrackableInventory = categoryDto.IsTrackableInventory;
            entity.Icon = categoryDto.Icon;
            entity.Color = categoryDto.Color;
            entity.Status = categoryDto.Status ?? entity.Status;

            _uow.CategoryRepository.Update(entity);
            await _uow.Complete();
            return mapper.Map<CategoryDto>(entity);
        }

        public async Task<CategoryDto> DeleteAsync(int categoryId, string? userId = null)
        {
            var entity = await _uow.CategoryRepository.GetByIdAsync(categoryId);
            if (entity == null || entity.IsDeleted)
            {
                throw new KeyNotFoundException("Category not found");
            }

            if (entity.IsDefault)
            {
                throw new InvalidOperationException("Default categories cannot be deleted");
            }

            if (entity.UserId != userId)
            {
                throw new UnauthorizedAccessException("You cannot delete this category");
            }

            entity.IsDeleted = true;
            _uow.CategoryRepository.Update(entity);
            await _uow.Complete();
            return mapper.Map<CategoryDto>(entity);
        }

        public async Task CreateMissingCategoriesAsync(IEnumerable<string> categoryNames)
        {
            var incomingCategories = categoryNames
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Distinct()
                .ToList();

            if (!incomingCategories.Any()) return;

// Lấy danh sách category hiện có trong database có tên trùng với incomingCategories
            var existingCategories = await _uow.CategoryRepository
                .FindAsync(c => incomingCategories.Contains(c.Name!));
// Lấy ra tên của các category đã tồn tại để so sánh
            var existingCategoryNames = existingCategories.Select(c => c.Name).ToList();

            var missingCategoryNames = incomingCategories.Except(existingCategoryNames).ToList();

            if (missingCategoryNames.Any())
            {
                var newCategories = missingCategoryNames
                    .Select(name => new Category 
                    { 
                        Name = name 
                    })
                    .ToList();

                await _uow.CategoryRepository.AddRangeAsync(newCategories);
                await _uow.Complete();
            }
        }
    }
}
