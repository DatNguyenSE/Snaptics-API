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
    ) : Controller
    {
        
        [HttpGet]
        [Authorize(Roles = "admin")]
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
            transactionDto.UserId = User.GetUserId();
            var transaction =
                await _transactionService.CreateAsync(transactionDto);
                return CreatedAtAction(nameof(GetTransaction), new { id = transaction.Id },transaction);
        }

        [HttpPost("from-bill")]
        public async Task<ActionResult<TransactionDto>> CreateFromBill([FromForm] BillReadResultDto billDto, IFormFile? image)
        {
            // Support Swagger and clients that send Items as a JSON string in a single form field
            if ((billDto.Items == null || !billDto.Items.Any()) && Request.HasFormContentType && Request.Form.TryGetValue("Items", out var itemsValues))
            {
                // Handle multiple "Items" form fields (e.g., from Swagger's "Add object item" button)
                var itemsString = itemsValues.Count > 1 
                    ? "[" + string.Join(",", itemsValues) + "]" 
                    : itemsValues.ToString();
                
                // If it's a single item but not wrapped in an array, wrap it
                if (!string.IsNullOrWhiteSpace(itemsString) && !itemsString.TrimStart().StartsWith("["))
                {
                    itemsString = $"[{itemsString}]";
                }

                if (!string.IsNullOrWhiteSpace(itemsString))
                {
                    try
                    {
                        billDto.Items = System.Text.Json.JsonSerializer.Deserialize<List<BillItemDto>>(
                            itemsString, 
                            new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true, AllowTrailingCommas = true }
                        ) ?? new List<BillItemDto>();
                    }
                    catch (System.Exception ex)
                    {
                        return BadRequest("Lỗi parse Items JSON: " + ex.Message + " | Input: " + itemsString);
                    }
                }
            }

            if (billDto == null ||
                string.IsNullOrWhiteSpace(billDto.MerchantName) ||
                billDto.Items == null ||
                !billDto.Items.Any())
            {
                return BadRequest("merchantName và Item không được để trống.");
            }
            
            if(image == null || image.Length == 0)
            {
                return BadRequest("Vui lòng chọn file ảnh.");
            }

            var userId = User.GetUserId();

         
            var billImageKey = await _s3Service.UploadFileAsync(image, "bill-images");
          

            var transaction =
                await _transactionService.CreateFromBillAsync(userId, billDto, billImageKey);

            return CreatedAtAction(
                nameof(GetTransaction),
                new { id = transaction.Id },
                transaction);
        }

        [HttpPost("from-analyze")]
        public async Task<ActionResult<TransactionDto>> CreateFromAnalyze([FromForm] AnalyzeImageResponseDto data, IFormFile? image)
        {   
            var imageDto = data;
            if (imageDto == null || string.IsNullOrWhiteSpace(imageDto.ItemName))
            {
                return BadRequest("itemName không được để trống.");
            }
            if (image == null || image.Length == 0)
            {
                return BadRequest("Vui lòng chọn file ảnh.");
            }

            var userId = User.GetUserId(); 

            var imageKey = await _s3Service.UploadFileAsync(image, "analyze-images");
        
            var transaction = await _transactionService.CreateFromImageAnalyzeAsync(userId, imageDto, imageKey);
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
                return BadRequest(ex.Message);
            }
        }
    }
}