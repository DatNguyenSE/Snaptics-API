using API.Extensions;
using BLL.Dtos;
using BLL.Interfaces.IServices;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Route("[controller]")]
    public class TransactionController(
        ITransactionService _transactionService
    ) : Controller
    {
        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<TransactionDto>>
        > GetTransactions()
        {
            var transactions =await _transactionService.GetAllAsync();
            return Ok(transactions);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<TransactionDto>
        > GetTransaction(int id)
        {
            var transaction = await _transactionService.GetByIdAsync(id);

            if (transaction == null)
            {
                return NotFound(
                    "Transaction not found"
                );
            }

            return Ok(transaction);
        }

        [HttpPost]
        public async Task<ActionResult<TransactionDto>
        > CreateTransaction([FromBody]TransactionDto transactionDto)
        {
            var transaction =
                await _transactionService.CreateAsync(transactionDto);
                return CreatedAtAction(nameof(GetTransaction), new { id = transaction.Id },transaction);
        }



        private string GetUserId()
        {
            // Tạm thời mock UserID cho quá trình test. Sau này lấy từ JWT: User.FindFirstValue(ClaimTypes.NameIdentifier)
            return "user-12345-mock-id";
        }

        [HttpPost("from-bill")]
        public async Task<ActionResult<TransactionDto>> CreateFromBill([FromBody] BLL.Dtos.AiDto.BillReadResultDto billDto)
        {   
            // var userId =User.GetUserId(); 
            var userId = GetUserId();
            var transaction = await _transactionService.CreateFromBillAsync(userId, billDto);
            return CreatedAtAction(nameof(GetTransaction), new { id = transaction.Id }, transaction);
        }

        [HttpPost("from-analyze")]
        public async Task<ActionResult<TransactionDto>> CreateFromAnalyze([FromBody] BLL.Dtos.AiDto.AnalyzeImageResponseDto imageDto)
        {   
            // var userId =User.GetUserId(); 
            var userId = GetUserId();
            var transaction = await _transactionService.CreateFromImageAnalyzeAsync(userId, imageDto);
            return CreatedAtAction(nameof(GetTransaction), new { id = transaction.Id }, transaction);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<TransactionDto>>UpdateTransaction(int id, [FromBody] TransactionDto transactionDto)
        {
            var updateTransaction = await _transactionService.UpdateAsync(id, transactionDto);
            return Ok(updateTransaction);
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult<TransactionDto>> DeleteTransaction(int id)
        {
            var deletedTransaction = await _transactionService.DeleteAsync(id);
            return Ok(deletedTransaction);
        }
    }
}