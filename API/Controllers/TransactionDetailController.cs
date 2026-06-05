using BLL.Interfaces.IServices;
using Microsoft.AspNetCore.Mvc;
using BLL.Dtos;

namespace API.Controllers
{
    [Route("[controller]")]
    public class TransactionDetailController(ITransactionDetailService _transactionDetailService) : Controller
    {
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TransactionDetailDto>>> GetTransactionDetails()
        {
            var transactionDetails = await _transactionDetailService.GetAllAsync();
            return Ok(transactionDetails);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<TransactionDetailDto>> GetTransactionDetail(int id)
        {
            var transactionDetail = await _transactionDetailService.GetByIdAsync(id);
            if (transactionDetail == null)
            {
                return NotFound("Transaction Detail not found");
            }
            return Ok(transactionDetail);
        }

        [HttpPost]
        public async Task<ActionResult<TransactionDetailDto>> CreateTransactionDetail([FromBody] TransactionDetailDto transactionDetailDto)
        {
            var transactionDetail = await _transactionDetailService.CreateAsync(transactionDetailDto);
            return CreatedAtAction(nameof(GetTransactionDetail), new { id = transactionDetail.Id }, transactionDetail);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<TransactionDetailDto>> UpdateTransactionDetail(int id,[FromBody] TransactionDetailDto transactionDetailDto)
        {
            if (id != transactionDetailDto.Id)
            {
                return BadRequest("ID trên đường dẫn không khớp với ID của chi tiết giao dịch.");
            }
            try
            {
                var result = await _transactionDetailService.UpdateAsync(id, transactionDetailDto);
                return Ok(result);

            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult<TransactionDetailDto>> DeleteTransactionDetail(int id)
        {
            try
            {
                var result = await _transactionDetailService.DeleteAsync(id);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

    }
}
