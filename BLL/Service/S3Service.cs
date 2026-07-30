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
            var region = string.IsNullOrEmpty(_aws.Region) ? "ap-southeast-1" : _aws.Region;
            var client = new AmazonS3Client(_aws.AccessKey, _aws.SecretKey, Amazon.RegionEndpoint.GetBySystemName(region));
            var extension = Path.GetExtension(file.FileName);

            var safeName = string.IsNullOrWhiteSpace(customerName)? "unknown": RemoveVietnamese(customerName).ToLower().Replace(" ", "");

            var fileName = $"{folder}/{safeName}_{DateTime.UtcNow.AddHours(7):dd-MM-yyyy_HH-mm-ss}{extension}";

            using var  stream = file.OpenReadStream();
            var bucketName = string.IsNullOrEmpty(_aws.BucketName) ? "s3-bucket-snaptics" : _aws.BucketName;
            var request = new PutObjectRequest
            {
                BucketName = bucketName,
                Key = fileName,
                InputStream = stream,
                ContentType = file.ContentType
            };
            await client.PutObjectAsync(request);
            return fileName;
        }

        public Task<string> GeneratePresignedUrlAsync(string key, int expiryMinutes = 15)
        {
            var region = string.IsNullOrEmpty(_aws.Region) ? "ap-southeast-1" : _aws.Region;
            var client = new AmazonS3Client(_aws.AccessKey, _aws.SecretKey, Amazon.RegionEndpoint.GetBySystemName(region));
            var bucketName = string.IsNullOrEmpty(_aws.BucketName) ? "s3-bucket-snaptics" : _aws.BucketName;
            var request = new GetPreSignedUrlRequest
            {
                BucketName = bucketName,
                Key = key,
                Expires = DateTime.UtcNow.AddMinutes(expiryMinutes)
            };
            return client.GetPreSignedURLAsync(request);
        }

        public async Task<byte[]> DownloadFileAsync(string key)
        {
            var region = string.IsNullOrEmpty(_aws.Region) ? "ap-southeast-1" : _aws.Region;
            var client = new AmazonS3Client(_aws.AccessKey, _aws.SecretKey, Amazon.RegionEndpoint.GetBySystemName(region));
            var bucketName = string.IsNullOrEmpty(_aws.BucketName) ? "s3-bucket-snaptics" : _aws.BucketName;
            var request = new Amazon.S3.Model.GetObjectRequest
            {
                BucketName = bucketName,
                Key = key
            };

            using var response = await client.GetObjectAsync(request);
            using var ms = new MemoryStream();
            await response.ResponseStream.CopyToAsync(ms);
            return ms.ToArray();
        }

        public async Task DeleteFileAsync(string key)
        {
            if (string.IsNullOrEmpty(key)) return;

            var region = string.IsNullOrEmpty(_aws.Region) ? "ap-southeast-1" : _aws.Region;
            var client = new AmazonS3Client(_aws.AccessKey, _aws.SecretKey, Amazon.RegionEndpoint.GetBySystemName(region));
            var bucketName = string.IsNullOrEmpty(_aws.BucketName) ? "s3-bucket-snaptics" : _aws.BucketName;
            var deleteRequest = new DeleteObjectRequest
            {
                BucketName = bucketName,
                Key = key
            };
            await client.DeleteObjectAsync(deleteRequest);
        }

        public async Task<string> MoveObjectAsync(string sourceKey, string destinationFolder = "bills")
        {
            if (string.IsNullOrEmpty(sourceKey)) return null;

            var region = string.IsNullOrEmpty(_aws.Region) ? "ap-southeast-1" : _aws.Region;
            var client = new AmazonS3Client(_aws.AccessKey, _aws.SecretKey, Amazon.RegionEndpoint.GetBySystemName(region));
            var bucketName = string.IsNullOrEmpty(_aws.BucketName) ? "s3-bucket-snaptics" : _aws.BucketName;
            
            // Extract filename from sourceKey (e.g., "temp-ai/image.jpg" -> "image.jpg")
            var fileName = Path.GetFileName(sourceKey);
            var destinationKey = $"{destinationFolder}/{fileName}";

            // Copy object
            var copyRequest = new CopyObjectRequest
            {
                SourceBucket = bucketName,
                SourceKey = sourceKey,
                DestinationBucket = bucketName,
                DestinationKey = destinationKey
            };
            await client.CopyObjectAsync(copyRequest);

            // Delete original object from temp folder
            var deleteRequest = new DeleteObjectRequest
            {
                BucketName = bucketName,
                Key = sourceKey
            };
            await client.DeleteObjectAsync(deleteRequest);

            return destinationKey;
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
