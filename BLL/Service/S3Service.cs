using BLL.Configurations;
using BLL.Interfaces.IServices;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Amazon.S3;
using Amazon.S3.Model;

namespace BLL.Service
{
    public class S3Service : IS3Service
    {
        private readonly AwsSettings _aws;
        public S3Service(IOptions<AwsSettings> options)
        {
            _aws = options.Value;
        }
        public async Task<string> UploadFileAsync(IFormFile file, string folder = "bills")
        {
            var client = new AmazonS3Client(_aws.AccessKey, _aws.SecretKey, Amazon.RegionEndpoint.GetBySystemName(_aws.Region));
            var fileName = $"{folder}/{Guid.NewGuid()}_{file.FileName}"; 
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
    }
}
