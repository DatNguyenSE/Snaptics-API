using BLL.Dtos;
using BLL.Interfaces.IServices;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace API.Controllers
{
    [Route("[controller]")]
    public class BudgetController(IBudgetService _budgetService) : Controller
    {
        private string GetUserId()
        {
            return "user-12345-mock-id";
        }

        // =========================================================
        [HttpGet("user")]
        public async Task<ActionResult<IEnumerable<BudgetDto>>> GetUserBudgets()
        {
            try
            {
                var userId = GetUserId();
                var budgets = await _budgetService.GetByUserIdAsync(userId);
                return Ok(budgets);
            }
            catch (System.Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpGet]
        public async Task<ActionResult<IEnumerable<BudgetDto>>> GetBudgets()
        {
            var budgets = await _budgetService.GetAllAsync();
            return Ok(budgets);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<BudgetDto>> GetBudget(int id)
        {
            var budget = await _budgetService.GetByIdAsync(id);
            if (budget == null)
            {
                return NotFound("Budget not found");
            }
            return Ok(budget);
        }

        [HttpPost]
        public async Task<ActionResult<BudgetDto>> CreateBudget([FromBody] BudgetDto budgetDto)
        {
            var budget = await _budgetService.CreateAsync(budgetDto);
            return CreatedAtAction(nameof(GetBudget), new { id = budget.Id }, budget);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<BudgetDto>> UpdateBudget(int id, [FromBody] BudgetDto budgetDto)
        {
            if (id != budgetDto.Id)
            {
                return BadRequest("ID mismatch");
            }
            try
            {
                var result = await _budgetService.UpdateAsync(id, budgetDto);
                return Ok(result);
            }
            catch (System.Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult<BudgetDto>> DeleteBudget(int id)
        {
            try
            {
                var result = await _budgetService.DeleteAsync(id);
                return Ok(result);
            }
            catch (System.Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpGet("history/{budgetId}")]
        public async Task<IActionResult> GetBudgetHistory(int budgetId)
        {
            try
            {
                // var userId = User.GetUserId(); 
                var userId = "user-123";

                var history = await _budgetService.GetBudgetHistoryAsync(userId, budgetId);

                return Ok(history);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }
    }
}
