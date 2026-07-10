using API.Extensions;
using BLL.Dtos;
using BLL.Interfaces.IServices;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Authorize]
    public class BudgetController(IBudgetService _budgetService) : Controller
    {

        // =========================================================
        [HttpGet("user")]
        public async Task<ActionResult<IEnumerable<BudgetDto>>> GetUserBudgets()
        {
            try
            {
                var userId = User.GetUserId();
                var budgets = await _budgetService.GetByUserIdAsync(userId);
                return Ok(budgets);
            }
            catch (System.Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpGet]
        [Authorize(Roles = "admin")]
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
            var userId = User.GetUserId();
            if(userId == null)
            {
                return Unauthorized("User ID not found in claims.");
            }

            try
            {
                var budget = await _budgetService.CreateAsync(userId, budgetDto);
                return CreatedAtAction(nameof(GetBudget), new { id = budget.Id }, budget);
            }
            catch (System.Exception ex)
            {
                return BadRequest(ex.Message);
            }
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
                var userId = User.GetUserId();
                if (userId == null)
                {
                    return Unauthorized("User ID not found in claims.");
                }

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
