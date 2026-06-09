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
    public class CategoryController(ICategoryService _cateService) : Controller
    {
        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<CategoryDto>>> GetCategories()
        {
            var categories = await _cateService.GetAllAsync();
            return Ok(categories);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<CategoryDto>> GetCategory(int id)
        {
            var category = await _cateService.GetByIdAsync(id);
            if (category == null)
            {
                return NotFound("Category not found");
            }
            return Ok(category);
        }
        [HttpPost]
        public async Task<ActionResult<CategoryDto>> CreateCategory([FromBody] CategoryDto categoryDto)
        {
            var category = await _cateService.CreateAsync(categoryDto);
            return CreatedAtAction(nameof(GetCategory), new { id = category.Id }, category);
        }
        [HttpPost("CreateByName")]
        public async Task<ActionResult<CategoryDto>> CreateCategoryByName([FromBody] string categoryName)
        {
            var category = await _cateService.CreateByNameAsync(categoryName);
            return CreatedAtAction(nameof(GetCategory), new { id = category.Id }, category);
        }
    }
}