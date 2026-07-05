using BLL.Dtos;
using BLL.Interfaces.IServices;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace API.Controllers
{
    [Route("[controller]")]
    public class BudgetController(IBudgetService _budgetService) : BaseController<BudgetController>
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
                Logger.LogError(ex, $"LỖI DB: Lấy danh sách Budget của user {GetUserId()} thất bại.");
                return StatusCode(500, "Đã xảy ra lỗi khi tải danh sách ngân sách.");
            }
        }
        [HttpGet]
        public async Task<ActionResult<IEnumerable<BudgetDto>>> GetBudgets()
        {
            try
            {
                var budgets = await _budgetService.GetAllAsync();
                return Ok(budgets);
            }
            catch (System.Exception ex)
            {
                Logger.LogError(ex, $"LỖI DB: Lấy toàn bộ danh sách Budget thất bại.");
                return StatusCode(500, "Đã xảy ra lỗi hệ thống.");
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<BudgetDto>> GetBudget(int id)
        {
            try
            {
                var budget = await _budgetService.GetByIdAsync(id);
                if (budget == null)
                {
                    return NotFound("Budget not found");
                }
                return Ok(budget);
            }
            catch (System.Exception ex)
            {
                Logger.LogError(ex, $"LỖI DB: Lấy thông tin Budget ID {id} thất bại.");
                return StatusCode(500, "Đã xảy ra lỗi hệ thống.");
            }
        }

        [HttpPost]
        public async Task<ActionResult<BudgetDto>> CreateBudget([FromBody] BudgetDto budgetDto)
        {
            try
            {
                var budget = await _budgetService.CreateAsync(budgetDto);

                Logger.LogInformation($"User {GetUserId()} vừa tạo Budget mới (ID: {budget.Id})");
                return CreatedAtAction(nameof(GetBudget), new { id = budget.Id }, budget);
            }
            catch (System.Exception ex)
            {
                Logger.LogError(ex, $"LỖI DB: Tạo Budget mới thất bại.");
                return StatusCode(500, "Không thể tạo ngân sách lúc này, vui lòng thử lại sau.");
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
                Logger.LogError(ex, $"LỖI DB: Cập nhật Budget ID {id} thất bại.");
                return StatusCode(500, "Lỗi hệ thống khi cập nhật ngân sách.");
            }
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult<BudgetDto>> DeleteBudget(int id)
        {
            try
            {
                var result = await _budgetService.DeleteAsync(id);
                Logger.LogInformation($"User {GetUserId()} vừa xóa Budget ID {id}.");
                return Ok(result);
            }
            catch (System.Exception ex)
            {
                Logger.LogError(ex, $"LỖI DB: Xóa Budget ID {id} thất bại.");
                return StatusCode(500, "Lỗi hệ thống khi xóa ngân sách.");
            }
        }

        [HttpPost("deduct")]
        public async Task<IActionResult> DeductMoney([FromBody] DeductMoneyDto dto)
        {
            try
            {
                // var userId = User.GetUserId(); 
                var userId = "user-123";

                var isSuccess = await _budgetService.DeductMoneyAsync(
                    userId,
                    dto.Amount,
                    dto.Note,
                    dto.BudgetId,
                    dto.IsAiEstimated);

                if (isSuccess)
                    return Ok(new { message = "Trừ tiền thành công và đã lưu lịch sử giao dịch!" });

                return BadRequest(new { message = "Giao dịch thất bại." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
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
