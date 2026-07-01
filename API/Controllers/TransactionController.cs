using API.Extensions;
using BLL.Dtos;
using BLL.Interfaces.IServices;
using BLL.Service;
using DAL.IRepositories;
using Microsoft.AspNetCore.Mvc;
using API.Extensions;
using Microsoft.AspNetCore.Authorization;
using BLL.Dtos.AiDto;

namespace API.Controllers
{
    [Authorize]
    [Route("[controller]")]
    public class TransactionController(
        ITransactionService _transactionService,
        IUnitOfWork _uow,
        IS3Service _s3Service
    ) : BaseController<TransactionController>
    {

        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<TransactionDto>>
        > GetTransactions()
        {
            try
            {
                var transactions = await _transactionService.GetAllAsync();
                return Ok(transactions);
            }
            catch (System.Exception ex)
            {
                Logger.LogError(ex, "Lỗi Db: Lấy thông tin giao dịch thất bại.");
                return StatusCode(500, "Đã xảy ra lỗi khi lấy thông tin giao dịch.");
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<TransactionDto>
        > GetTransaction(int id)
        {
            try
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
            catch (System.Exception ex)
            {
                Logger.LogError(ex, $"Lỗi Db: Lấy thông tin giao dịch (ID: {id}) thất bại.");
                return StatusCode(500, "Đã xảy ra lỗi khi lấy thông tin giao dịch.");
            }
        }

        [HttpPost]
        public async Task<ActionResult<TransactionDto>
        > CreateTransaction([FromBody] TransactionDto transactionDto)
        {
            try
            {
                transactionDto.UserId = User.GetUserId();
                var transaction =
                    await _transactionService.CreateAsync(transactionDto);
                Logger.LogInformation($"Tạo thành công giao dịch mới (ID: {transaction.Id})");
                return CreatedAtAction(nameof(GetTransaction), new { id = transaction.Id }, transaction);
            }
            catch (System.Exception ex)
            {
                Logger.LogError(ex, "Lỗi Db: Tạo giao dịch mới thất bại.");
                return StatusCode(500, "Đã xảy ra lỗi khi tạo giao dịch mới.");
            }
        }

        [HttpPost("from-bill")]
        public async Task<ActionResult<TransactionDto>> CreateFromBill([FromBody] BillReadResultDto billDto)
        {
            if (billDto == null ||
                string.IsNullOrWhiteSpace(billDto.MerchantName) ||
                billDto.Items == null ||
                !billDto.Items.Any())
            {
                return BadRequest("merchantName và Item không được để trống.");
            }

            try
            {
                var userId = User.GetUserId();

                var transaction =
                    await _transactionService.CreateFromBillAsync(userId, billDto);

                Logger.LogInformation($"User {userId} vừa tạo giao dịch thành công từ Bill quét tự động.");
                return CreatedAtAction(
                    nameof(GetTransaction),
                    new { id = transaction.Id },
                    transaction);
            }
            catch (System.Exception ex)
            {
                Logger.LogError(ex, "Lỗi Db: Tạo giao dịch từ hóa đơn thất bại.");
                return StatusCode(500, "Đã xảy ra lỗi khi tạo giao dịch từ hóa đơn.");
            }
        }

        [HttpPost("from-analyze")]
        public async Task<ActionResult<TransactionDto>> CreateFromAnalyze([FromForm] BLL.Dtos.AiDto.AnalyzeImageResponseDto data, IFormFile? image)
        {
            var imageDto = data;
            if (imageDto == null || string.IsNullOrWhiteSpace(imageDto.ItemName))
            {
                return BadRequest("itemName không được để trống.");
            }
            try
            {
                var userId = User.GetUserId();
                var transaction = await _transactionService.CreateFromImageAnalyzeAsync(userId, imageDto);
                return CreatedAtAction(nameof(GetTransaction), new { id = transaction.Id }, transaction);
            }
            catch (System.Exception ex)
            {
                Logger.LogError(ex, "LỖI DB: Tạo giao dịch từ ảnh phân tích AI thất bại.");
                return StatusCode(500, "Đã xảy ra lỗi khi tạo giao dịch từ phân tích hình ảnh.");
            }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<TransactionDto>> UpdateTransaction(int id, [FromBody] TransactionDto transactionDto)
        {
            try
            {
                var updateTransaction = await _transactionService.UpdateAsync(id, transactionDto);
                return Ok(updateTransaction);
            }
            catch (System.Exception ex)
            {
                Logger.LogError(ex, "LỖI DB: Cập nhật giao dịch thất bại.");
                return StatusCode(500, "Đã xảy ra lỗi khi cập nhật giao dịch.");
            }
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult<TransactionDto>> DeleteTransaction(int id)
        {
            try
            {
                var deletedTransaction = await _transactionService.DeleteAsync(id);
                Logger.LogInformation($"Xóa thành công giao dịch (ID: {id})");
                return Ok(deletedTransaction);
            }
            catch (System.Exception ex)
            {
                Logger.LogError(ex, "LỖI DB: Xóa giao dịch thất bại.");
                return StatusCode(500, "Đã xảy ra lỗi khi xóa giao dịch.");
            }
        }

        [HttpPut("{transactionId}/confirm-prices")]
        public async Task<IActionResult> ConfirmPrices(int transactionId, [FromBody] List<BLL.Dtos.UpdateItemPriceDto> itemsDto)
        {
            try
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

                Logger.LogInformation($"Giao dịch (ID: {transactionId}) đã được confirm lại giá thành công.");
                return Ok(new { Message = "Xác nhận giá thành công!", TotalAmount = newTotalAmount });

            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"LỖI DB: Lỗi tính toán và confirm giá cho giao dịch (ID: {transactionId}).");
                return StatusCode(500, "Lỗi hệ thống khi xác nhận giá tiền, vui lòng thử lại.");
            }
        }

        [HttpPost("test-trigger-missing-price-scan")]
        public async Task<IActionResult> TestTriggerMissingPriceScan(
            [FromServices] IMissingPriceJob missingPriceJob)
        {
            try
            {
                Logger.LogInformation("[JOB TRIGGER] Bắt đầu quét các giao dịch bị thiếu giá tiền...");
                await missingPriceJob.ScanAndSendNotificationAsync();

                Logger.LogInformation("[JOB TRIGGER] Quét các giao dịch bị thiếu giá tiền hoàn tất.");
                return Ok(new
                {
                    Message = "Manual trigger for missing price scan executed successfully."
                });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "[JOB THẤT BẠI] Quét giao dịch thiếu giá tiền bị sập!");
                return StatusCode(500, "Lỗi hệ thống khi chạy tiến trình quét giá tiền.");
            }

        }
        [HttpGet("user")]
        public async Task<ActionResult<IEnumerable<TransactionDto>>> GetUserTransactions()
        {
            try
            {
                var userId = User.GetUserId();
                var transactions = await _transactionService.GetByUserIdAsync(userId);
                return Ok(transactions);
            }
            catch (System.Exception ex)
            {
                Logger.LogError(ex, $"LỖI DB: Lấy danh sách giao dịch của user {User.GetUserId()} thất bại.");
                return StatusCode(500, "Không thể tải danh sách giao dịch lúc này.");
            }
        }
    }
}