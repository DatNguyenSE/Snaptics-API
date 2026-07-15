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
            var result = await incomeSourceService.CreateAsync(dto);

            return CreatedAtAction(
                nameof(GetIncomeSource),
                new { id = result.Id },
                result
            );
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
