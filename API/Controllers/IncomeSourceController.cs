using API.Extensions;
using BLL.Dtos;
using BLL.Interfaces.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Route("[controller]")]
    [Authorize]
    public class IncomeSourceController(IIncomeSourceService incomeSourceService) : Controller
    {
        [HttpGet]
        public async Task<ActionResult<IEnumerable<IncomeSourceDto>>> GetIncomeSources()
        {
            var incomes = await incomeSourceService.GetAllAsync();
            return Ok(incomes);
        }

        [HttpGet("user")]
        public async Task<ActionResult<IEnumerable<IncomeSourceDto>>> GetUserIncomeSources()
        {
            try
            {
                var userId = User.GetUserId();
                if (userId == null)
                {
                    return Unauthorized("User ID not found in claims.");
                }

                var incomes = await incomeSourceService.GetByUserIdAsync(userId);
                return Ok(incomes);
            }
            catch (System.Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<IncomeSourceDto>> GetIncomeSource(int id)
        {
            var income = await incomeSourceService.GetByIdAsync(id);

            if (income == null)
            {
                return NotFound("Income source not found");
            }

            return Ok(income);
        }

        [HttpPost]
        public async Task<ActionResult<IncomeSourceDto>> CreateIncomeSource(
            [FromBody] IncomeSourceDto dto)
        {
            var userId = User.GetUserId();
            if (userId == null)
            {
                return Unauthorized("User ID not found in claims.");
            }

            try
            {
                var result = await incomeSourceService.CreateAsync(userId, dto);

                return CreatedAtAction(
                    nameof(GetIncomeSource),
                    new { id = result.Id },
                    result
                );
            }
            catch (System.Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<IncomeSourceDto>> UpdateIncomeSource(
            int id,
            [FromBody] IncomeSourceDto dto)
        {
            var result = await incomeSourceService.UpdateAsync(id, dto);

            return Ok(result);
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult<IncomeSourceDto>> DeleteIncomeSource(int id)
        {
            var result = await incomeSourceService.DeleteAsync(id);

            return Ok(result);
        }
    }
}
