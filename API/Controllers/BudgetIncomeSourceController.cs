using BLL.Dtos;
using BLL.Interfaces.IServices;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BudgetIncomeSourceController(IBudgetIncomeSourceService _service) : ControllerBase
    {

        // Lấy tất cả IncomeSource của một Budget
        [HttpGet("budget/{budgetId}")]
        public async Task<IActionResult> GetByBudgetId(int budgetId)
        {
            var result = await _service.GetByBudgetIdAsync(budgetId);
            return Ok(result);
        }

        // Thêm IncomeSource vào Budget
        [HttpPost]
        public async Task<IActionResult> Add(BudgetIncomeSourceDto dto)
        {
            var result = await _service.AddAsync(dto);
            return Ok(result);
        }

        // Cập nhật số tiền hoặc IncomeSource
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, BudgetIncomeSourceDto dto)
        {
            var result = await _service.UpdateAsync(id, dto);
            return Ok(result);
        }

        // Xóa IncomeSource khỏi Budget
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _service.DeleteAsync(id);
            return NoContent();
        }
    }
}