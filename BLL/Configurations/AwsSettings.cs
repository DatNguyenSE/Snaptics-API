using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Http;

namespace BLL.Configurations
{
    public class AwsSettings
    {
        public string AccessKey { get; set; } = string.Empty;
        public string SecretKey { get; set; } = string.Empty;
        public string BucketName { get; set; } = string.Empty;
        public string Region { get; set; } = string.Empty;
    }
}
