using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Http;

namespace BLL.Interfaces.IServices
{
    public interface IS3Service
    {
        //tải lên s3 trong thư mục mặc định "bills" or "analyze-images" , trả về key 
        Task<string> UploadFileAsync(IFormFile file, string folder = "bills");
        //tạo url tạm thời để xem từ 'key' file đã tải lên, mặc định hết hạn sau 15 phút
        Task<string> GeneratePresignedUrlAsync(string key, int expiryMinutes = 15);
    }
}
