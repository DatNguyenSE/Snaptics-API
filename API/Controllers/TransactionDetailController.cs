using BLL.Interfaces.IServices;
using Microsoft.AspNetCore.Mvc;
using BLL.Dtos;

namespace API.Controllers
{
    [Route("[controller]")]
    public class TransactionDetailController(ITransactionDetailService _transactionDetailService) : BaseController<TransactionDetailController>
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

    }
}
