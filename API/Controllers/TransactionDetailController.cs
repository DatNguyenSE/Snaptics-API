using BLL.Interfaces.IServices;
using Microsoft.AspNetCore.Mvc;
using BLL.Dtos;
using DAL.IRepositories;

namespace API.Controllers
{
    [Route("[controller]")]
    public class TransactionDetailController(ITransactionDetailService _transactionDetailService,
                IUnitOfWork _uow) : Controller
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

        [HttpPut("transaction/{transactionId}/confirm-prices")]
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

        [HttpGet("transaction/{transactionId}/missing-prices")]
        public async Task<IActionResult> GetMissingPriceDetails(int transactionId)
        {
            // Bước 1: Lấy toàn bộ danh sách details lên (dựa vào hàm ông đang có)
            var allDetails = await _transactionDetailService.GetAllAsync();

            var missingDetails = allDetails
                .Where(td => td.TransactionId == transactionId && td.Price == 0)
                .ToList();

            if (missingDetails.Count == 0)
            {
                return NotFound(new { Message = "Không tìm thấy món hàng nào thiếu giá trong hóa đơn này!" });
            }

            return Ok(missingDetails);
        }

    }
}
