using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Http;

namespace BLL.Interfaces.IServices
{
    public interface IS3Service
    {
        Task<string> UploadFileAsync(IFormFile file, string folder = "bills");
        Task<string> GeneratePresignedUrlAsync(string key, int expiryMinutes = 15);
    }
}
