using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using BLL.Configurations;
using BLL.Interfaces.IServices;
using Microsoft.Extensions.Options;

namespace BLL.Service
{
    public class SnsService : ISnsService
    {
        private readonly AwsSettings _aws;
        private readonly AwsSnsSettings _snsSettings;

        public SnsService(
            IOptions<AwsSettings> awsOptions,
            IOptions<AwsSnsSettings> snsOptions)
        {
            _aws = awsOptions.Value;
            _snsSettings = snsOptions.Value;
        }

        public async Task PublishAsync(
            string subject,
            string message)
        {
            var client =
                new AmazonSimpleNotificationServiceClient(
                    _aws.AccessKey,
                    _aws.SecretKey,
                    Amazon.RegionEndpoint.GetBySystemName(_aws.Region));

            await client.PublishAsync(
                new PublishRequest
                {
                    TopicArn = _snsSettings.TopicArn,
                    Subject = subject,
                    Message = message
                });
        }
    }
}