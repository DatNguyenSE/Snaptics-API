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
            return mapper.Map<IEnumerable<CategoryDto>>(cates);
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

        public async Task<CategoryDto> CreateByNameAsync(string categoryName)
        {
            var isDuplicate = await _uow.CategoryRepository.AnyAsync(c => c.Name == categoryName);
            if (isDuplicate)
            {
                throw new InvalidOperationException("Category already exists");
            }

            var entity = new Category { Name = categoryName };
            await _uow.CategoryRepository.AddAsync(entity);
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