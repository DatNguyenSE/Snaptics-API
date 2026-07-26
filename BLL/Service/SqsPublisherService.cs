using System.Text.Json;
using Amazon.SQS;
using Amazon.SQS.Model;
using BLL.Interfaces.IServices;

namespace BLL.Service
{
    public class SqsPublisherService : ISqsPublisherService
    {
        private readonly IAmazonSQS _sqsClient;

        public SqsPublisherService(IAmazonSQS sqsClient)
        {
            _sqsClient = sqsClient;
        }

        public async Task SendMessageAsync<T>(T message, string queueName = "snaptics-ai-queue")
        {
            var queueUrlResponse = await _sqsClient.GetQueueUrlAsync(queueName);
            var queueUrl = queueUrlResponse.QueueUrl;

            var messageBody = JsonSerializer.Serialize(message);

            var sendRequest = new SendMessageRequest
            {
                QueueUrl = queueUrl,
                MessageBody = messageBody
            };

            await _sqsClient.SendMessageAsync(sendRequest);
        }
    }
}
