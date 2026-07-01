using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using BLL.Dtos;
using BLL.Interfaces.IServices;
using BLL.Service;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace API.Controllers
{
    [Route("[controller]")]
    public class CategoryController(ICategoryService _cateService) : BaseController<CategoryController>
    {
        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<CategoryDto>>> GetCategories()
        {
            try
            {
                var categories = await _cateService.GetAllAsync();
                return Ok(categories);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "LỖI DB: Lấy danh sách Category thất bại.");
                return StatusCode(500, "Đã xảy ra lỗi khi tải danh sách danh mục.");
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<CategoryDto>> GetCategory(int id)
        {
            try
            {
                var category = await _cateService.GetByIdAsync(id);
                if (category == null)
                {
                    return NotFound("Category not found");
                }
                return Ok(category);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"LỖI DB: Lấy thông tin Category (ID: {id}) thất bại.");
                return StatusCode(500, "Đã xảy ra lỗi khi tải thông tin danh mục.");
            }
        }
        [HttpPost]
        public async Task<ActionResult<CategoryDto>> CreateCategory([FromBody] CategoryDto categoryDto)
        {
            try
            {
                var category = await _cateService.CreateAsync(categoryDto);
                Logger.LogInformation($"Tạo thành công Category mới (ID: {category.Id})");
                return CreatedAtAction(nameof(GetCategory), new { id = category.Id }, category);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "LỖI DB: Tạo Category mới bằng DTO thất bại.");
                return StatusCode(500, "Không thể tạo danh mục lúc này, vui lòng thử lại.");
            }
        }
        [HttpPost("CreateByName")]
        public async Task<ActionResult<CategoryDto>> CreateCategoryByName([FromBody] string categoryName)
        {
            try
            {
                var category = await _cateService.CreateByNameAsync(categoryName);
                Logger.LogInformation($"Tạo thành công Category mới (ID: {category.Id})");
                return CreatedAtAction(nameof(GetCategory), new { id = category.Id }, category);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"LỖI DB: Tạo Category theo tên '{categoryName}' thất bại.");
                return StatusCode(500, "Không thể tạo danh mục lúc này, vui lòng thử lại.");
            }
        }
    }
}