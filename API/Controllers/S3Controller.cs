using BLL.Interfaces.IServices;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Route("s3")]
    [ApiController]
    public class S3Controller(IS3Service _s3Service) : BaseController<S3Controller>
    {
        [HttpPost("upload")]
        public async Task<IActionResult> Upload(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("File is required");
            try
            {
                Logger.LogInformation("Uploading file: {FileName}", file.FileName);
                var url = await _s3Service.UploadFileAsync(file, file.FileName);

                return Ok(new
                {
                    Message = "Upload success",
                    Url = url
                });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"LỖI S3: Upload file {file.FileName} thất bại.");
                return StatusCode(500, "Lỗi hệ thống khi tải file lên, vui lòng thử lại.");
            }
        }

        [HttpGet("view")]
        public async Task<IActionResult> ViewFile([FromQuery] string key)
        {
            // if (string.IsNullOrEmpty(key)) return BadRequest("Key is required");
            try
            {
                var url = await _s3Service.GeneratePresignedUrlAsync(key);
                return Ok(new
                {
                    Url = url
                });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"LỖI S3: Không thể tạo Presigned URL cho key: {key}");
                return StatusCode(500, "Lỗi hệ thống khi lấy link file.");
            }
        }

        [HttpGet("image")]
        public async Task<IActionResult> GetImage([FromQuery] string key)
        {
            if (string.IsNullOrEmpty(key)) return BadRequest("Key is required");
            try
            {
                var url = await _s3Service.GeneratePresignedUrlAsync(key);
                return Redirect(url);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"LỖI S3: Không thể tạo Presigned URL cho hình ảnh với key: {key}");
                return StatusCode(500, "Lỗi hệ thống khi lấy link hình ảnh.");
            }
        }
    }
}