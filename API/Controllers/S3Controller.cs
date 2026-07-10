using BLL.Interfaces.IServices;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace API.Controllers
{
    [Route("s3")]
    [ApiController]
    public class S3Controller(IS3Service _s3Service) : ControllerBase
    {
        [HttpPost("upload")]
        [Authorize]
        public async Task<IActionResult> Upload(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("File is required");

            var url = await _s3Service.UploadFileAsync(file, file.FileName);

            return Ok(new
            {
                Message = "Upload success",
                Url = url
            });
        }

        [HttpGet("view")]
        public async Task<IActionResult> ViewFile([FromQuery] string key)
        {
            var url = await _s3Service.GeneratePresignedUrlAsync(key);
            return Ok(new
            {
                Url = url
            });
        }

        [HttpGet("image")]
        public async Task<IActionResult> GetImage([FromQuery] string key)
        {
            if (string.IsNullOrEmpty(key)) return BadRequest("Key is required");
            var url = await _s3Service.GeneratePresignedUrlAsync(key);
            return Redirect(url);
        }
    }
}