using AutoMapper;
using BLL.Dtos;
using BLL.Interfaces.IServices;
using DAL.Entities;
using DAL.IRepositories;

namespace BLL.Service
{
    public class CategoryService(IUnitOfWork _uow, IMapper mapper) : ICategoryService
    {
        public async Task<IEnumerable<CategoryDto>> GetAllAsync()
        {
            var cates = await _uow.CategoryRepository.GetAllAsync();
            return mapper.Map<IEnumerable<CategoryDto>>(cates);
             // use AutoMapper to map from Category to CategoryDto (need to configure mapping in AutoMapper profile)
        }

        public async Task<CategoryDto> GetByIdAsync(int categoryId)
        {
            var cate = await _uow.CategoryRepository.GetByIdAsync(categoryId);
            return mapper.Map<CategoryDto>(cate);
        }

        public async Task<CategoryDto> CreateAsync(CategoryDto categoryDto)
        {
            var isDuplicate = await _uow.CategoryRepository.AnyAsync(c => c.Name == categoryDto.Name);
            if (isDuplicate)
            {
                throw new InvalidOperationException("Category already exists");
            }

            // Map lại từ dto sang entity để lưu vào database
            var entity = mapper.Map<Category>(categoryDto);
            await _uow.CategoryRepository.AddAsync(entity);
           
            await _uow.Complete();
            return mapper.Map<CategoryDto>(entity);
        }
    }
}