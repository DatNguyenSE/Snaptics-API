using BLL.Interfaces.IServices;
using Microsoft.AspNetCore.Mvc;
using BLL.Dtos;
using DAL.IRepositories;

namespace API.Controllers
{
    [Route("[controller]")]
    public class TransactionDetailController(ITransactionDetailService _transactionDetailService,
                IUnitOfWork _uow) : BaseController<TransactionDetailController>
    {

        [HttpGet]
        public async Task<ActionResult<IEnumerable<TransactionDetailDto>>> GetTransactionDetails()
        {
            try
            {
                var transactionDetails = await _transactionDetailService.GetAllAsync();
                return Ok(transactionDetails);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Lỗi Db: Lấy thông tin chi tiết giao dịch thất bại.");
                return StatusCode(500, "Đã xảy ra lỗi khi lấy thông tin chi tiết giao dịch.");
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<TransactionDetailDto>> GetTransactionDetail(int id)
        {
            try
            {
                var transactionDetail = await _transactionDetailService.GetByIdAsync(id);
                if (transactionDetail == null)
                {
                    return NotFound("Transaction Detail not found");
                }
                return Ok(transactionDetail);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"Lỗi Db: Lấy thông tin chi tiết giao dịch (ID: {id}) thất bại.");
                return StatusCode(500, "Đã xảy ra lỗi khi lấy thông tin chi tiết giao dịch.");
            }
        }

        [HttpPost]
        public async Task<ActionResult<TransactionDetailDto>> CreateTransactionDetail([FromBody] TransactionDetailDto transactionDetailDto)
        {
            try
            {
                var transactionDetail = await _transactionDetailService.CreateAsync(transactionDetailDto);
                Logger.LogInformation($"Tạo thành công chi tiết giao dịch mới (ID: {transactionDetail.Id})");
                return CreatedAtAction(nameof(GetTransactionDetail), new { id = transactionDetail.Id }, transactionDetail);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Lỗi Db: Tạo chi tiết giao dịch mới thất bại.");
                return StatusCode(500, "Đã xảy ra lỗi khi tạo chi tiết giao dịch mới.");
            }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<TransactionDetailDto>> UpdateTransactionDetail(int id, [FromBody] TransactionDetailDto transactionDetailDto)
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
                Logger.LogError(ex, $"Lỗi Db: Cập nhật chi tiết giao dịch (ID: {id}) thất bại.");
                return StatusCode(500, "Đã xảy ra lỗi khi cập nhật chi tiết giao dịch.");
            }
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult<TransactionDetailDto>> DeleteTransactionDetail(int id)
        {
            try
            {
                var result = await _transactionDetailService.DeleteAsync(id);
                Logger.LogInformation($"Xóa thành công chi tiết giao dịch (ID: {id})");
                return Ok(result);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"Lỗi Db: Xóa chi tiết giao dịch (ID: {id}) thất bại.");
                return StatusCode(500, "Đã xảy ra lỗi khi xóa chi tiết giao dịch.");
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
