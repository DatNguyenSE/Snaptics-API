using BLL.Configurations;
using BLL.Interfaces.IServices;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Amazon.S3;
using Amazon.S3.Model;
using System.Globalization;
using System.Text;

namespace BLL.Service
{
    public class S3Service : IS3Service
    {
        private readonly AwsSettings _aws;
        public S3Service(IOptions<AwsSettings> options)
        {
            _aws = options.Value;
        }
        public async Task<string> UploadFileAsync(IFormFile file, string customerName, string folder = "bills")
        {
            var client = new AmazonS3Client(_aws.AccessKey, _aws.SecretKey, Amazon.RegionEndpoint.GetBySystemName(_aws.Region));
            var extension = Path.GetExtension(file.FileName);

            var safeName = string.IsNullOrWhiteSpace(customerName)? "unknown": RemoveVietnamese(customerName).ToLower().Replace(" ", "");

            var fileName = $"{folder}/{safeName}_{DateTime.Now:dd-MM-yyyy_HH-mm-ss}{extension}";

            using var  stream = file.OpenReadStream();
            var request = new PutObjectRequest
            {
                BucketName = _aws.BucketName,
                Key = fileName,
                InputStream = stream,
                ContentType = file.ContentType
            };
            await client.PutObjectAsync(request);
            return fileName;
        }

        public Task<string> GeneratePresignedUrlAsync(string key, int expiryMinutes = 15)
        {
            var client = new AmazonS3Client(_aws.AccessKey, _aws.SecretKey, Amazon.RegionEndpoint.GetBySystemName(_aws.Region));
            var request = new GetPreSignedUrlRequest
            {
                BucketName = _aws.BucketName,
                Key = key,
                Expires = DateTime.UtcNow.AddMinutes(expiryMinutes)
            };
            return client.GetPreSignedURLAsync(request);
        }

        public static string RemoveVietnamese(string text)
        {
            var normalized = text.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();

            foreach (var c in normalized)
            {
                var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != UnicodeCategory.NonSpacingMark)
                {
                    sb.Append(c);
                }
            }
            return sb.ToString().Replace('đ', 'd').Replace('Đ', 'D').Normalize(NormalizationForm.FormC);
        }
    }
}
