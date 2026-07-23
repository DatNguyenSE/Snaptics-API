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
                if (userId == null) return Unauthorized("User ID not found in claims.");
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
            if (userId == null)
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

        [AllowAnonymous]
        [HttpGet("history-budgets")]
        public async Task<ActionResult<IEnumerable<object>>> GetInactiveBudgetsHistory()
        {
            try
            {
                var userId = User.GetUserId();
                if (userId == null) return Unauthorized("User ID not found in claims.");

                var budgets = await _budgetService.GetByUserIdAsync(userId);

                var history = budgets.Where(b => !b.IsActive).Select(b => new
                {
                    b.Id,
                    b.Name,
                    b.Amount,
                    b.CurrentAmount,
                    Month = b.StartDate.Month,
                    Year = b.StartDate.Year,
                    LinkedFromBudgetId = b.PreviousBudgetId
                }).ToList();

                return Ok(history);
            }
            catch (System.Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [AllowAnonymous]
        [HttpPost("trigger-rollover")]
        public async Task<IActionResult> TriggerRolloverNow()
        {
            try
            {
                await _budgetService.ProcessPeriodicRolloverAsync();
                return Ok(new { message = "Chốt sổ ví Periodic thành công!" });
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [AllowAnonymous]
        [HttpPatch("{id}/toggle-autorenew")]
        public async Task<IActionResult> ToggleAutoRenew(int id)
        {
            try
            {
                var existingBudget = await _budgetService.GetByIdAsync(id);
                if (existingBudget == null) return NotFound("Không tìm thấy ví.");

                // Đảo ngược trạng thái ( true thành false,  false thành true)
                existingBudget.IsAutoRenew = !existingBudget.IsAutoRenew;

                await _budgetService.UpdateAsync(id, existingBudget);

                return Ok(new
                {
                    message = existingBudget.IsAutoRenew ? "Đã BẬT gia hạn tự động" : "Đã TẮT gia hạn tự động",
                    isAutoRenew = existingBudget.IsAutoRenew
                });
            }
            catch (System.Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
