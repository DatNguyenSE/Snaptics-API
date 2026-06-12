using API.Extensions;
using BLL.Dtos;
using BLL.Interfaces.IServices;
using BLL.Service;
using DAL.IRepositories;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    
    [Route("[controller]")]
    public class TransactionController(
        ITransactionService _transactionService,
        IUnitOfWork _uow
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

        [HttpPut("{transactionId}/confirm-prices")]
        public async Task<IActionResult> ConfirmPrices(int transactionId, [FromBody] List<BLL.Dtos.UpdateItemPriceDto> itemsDto)        
        {
            var transaction = await _uow.TransactionRepository.GetByIdAsync(transactionId);
            if (transaction == null || transaction.IsDeleted) 
                return NotFound(new { Message = "Không tìm thấy hóa đơn." });

            decimal newTotalAmount = transaction.TotalAmount; 
            // Lặp qua từng món user gửi lên
            foreach (var itemDto in itemsDto)
            {
                // Lấy trực tiếp từng món đồ lên từ Database thông qua ID của nó
                // (Sử dụng TransactionDetailRepository để bypass lỗi thiếu Include)
                var dbItem = await _uow.TransactionDetailRepository.GetByIdAsync(itemDto.DetailId);
                
                // Đảm bảo món đồ này có tồn tại và thuộc về đúng cái hóa đơn 
                if (dbItem != null && dbItem.TransactionId == transactionId)
                {
                    // 1. Trừ đi tiền cũ (lỡ có) của món đồ này khỏi tổng tiền
                    newTotalAmount -= (dbItem.Price * dbItem.Quantity);
                    
                    // 2. Gán giá mới bằng giá  gửi lên
                    dbItem.Price = itemDto.Price;
                    _uow.TransactionDetailRepository.Update(dbItem);
                    
                    // 3. Cộng tiền mới dội ngược lại vào tổng tiền
                    newTotalAmount += (dbItem.Price * dbItem.Quantity);
                }
            }

            // Cập nhật tổng tiền cuối cùng
            transaction.TotalAmount = newTotalAmount; 
            
            // LẬT CỜ
            transaction.IsAiEstimated = true; 

            _uow.TransactionRepository.Update(transaction);
            await _uow.Complete();

            return Ok(new { Message = "Xác nhận giá thành công!", TotalAmount = newTotalAmount });
        }

        [HttpPost("test-trigger-missing-price-scan")]
        public async Task<IActionResult> TestTriggerMissingPriceScan(
            [FromServices] IMissingPriceJob missingPriceJob) 
        {
            await missingPriceJob.ScanAndSendNotificationAsync(); 
            
            return Ok(new 
            { 
                Message = "Manual trigger for missing price scan executed successfully." 
            });
        }
    }
}