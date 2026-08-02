using System.Text.Json;
using Amazon.SQS;
using Amazon.SQS.Model;
using API.Hubs;
using BLL.Dtos.AiDto;
using BLL.Interfaces.IServices;
using Microsoft.AspNetCore.SignalR;

namespace API.BackgroundServices
{
    public class SqsConsumerService : BackgroundService
    {
        private readonly IAmazonSQS _sqsClient;
        private readonly IServiceProvider _serviceProvider;
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly ILogger<SqsConsumerService> _logger;
        private const string QueueName = "snaptics-ai-queue";

        public SqsConsumerService(IAmazonSQS sqsClient, IServiceProvider serviceProvider, IHubContext<NotificationHub> hubContext, ILogger<SqsConsumerService> logger)
        {
            _sqsClient = sqsClient;
            _serviceProvider = serviceProvider;
            _hubContext = hubContext;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("SQS Consumer Background Service is starting.");

            string? queueUrl = null;

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    if (string.IsNullOrEmpty(queueUrl))
                    {
                        var queueUrlResponse = await _sqsClient.GetQueueUrlAsync(QueueName, stoppingToken);
                        queueUrl = queueUrlResponse.QueueUrl;
                    }

                    var receiveRequest = new ReceiveMessageRequest
                    {
                        QueueUrl = queueUrl,
                        MaxNumberOfMessages = 1,
                        WaitTimeSeconds = 10 // Long polling
                    };

                    var response = await _sqsClient.ReceiveMessageAsync(receiveRequest, stoppingToken);

                    if (response?.Messages != null)
                    {
                        foreach (var message in response.Messages)
                        {
                            _logger.LogInformation($"Received SQS message: {message.MessageId}");
                            await ProcessMessageAsync(message.Body, stoppingToken);

                            // Delete message after processing
                            var deleteRequest = new DeleteMessageRequest
                            {
                                QueueUrl = queueUrl,
                                ReceiptHandle = message.ReceiptHandle
                            };
                            await _sqsClient.DeleteMessageAsync(deleteRequest, stoppingToken);
                        }
                    }
                }
                catch (AmazonSQSException ex)
                {
                    _logger.LogError(ex, "AWS SQS Error. Queue might not exist yet.");
                    await Task.Delay(5000, stoppingToken); // Wait before retrying
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing SQS message.");
                }
            }
        }

        private async Task ProcessMessageAsync(string messageBody, CancellationToken stoppingToken)
        {
            var aiTask = JsonSerializer.Deserialize<AiTaskMessageDto>(messageBody);
            if (aiTask == null) return;

            using var scope = _serviceProvider.CreateScope();
            var aiService = scope.ServiceProvider.GetRequiredService<IAiService>();
            var s3Service = scope.ServiceProvider.GetRequiredService<IS3Service>();

            try
            {
                _logger.LogInformation($"Processing AI task {aiTask.TaskType} for User: {aiTask.UserId}");

                // 1. Tải ảnh từ S3
                var imageBytes = await s3Service.DownloadFileAsync(aiTask.S3ObjectKey);

                // 2. Gọi AI xử lý
                object result = null;
                if (aiTask.TaskType == "AnalyzeImage")
                {
                    result = await aiService.AnalyzeImageAsync(imageBytes, aiTask.ContentType, aiTask.UserId, aiTask.EstimatePrice);
                }
                else if (aiTask.TaskType == "ReadBill")
                {
                    result = await aiService.ReadBillAsync(imageBytes, aiTask.ContentType);
                }

                // 3. Gửi thông báo hoàn thành qua SignalR
                await _hubContext.Clients.User(aiTask.UserId).SendAsync("ReceiveAiResult", result, cancellationToken: stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to process AI task for User: {aiTask.UserId}");
                await _hubContext.Clients.User(aiTask.UserId).SendAsync("ReceiveAiError", "Lỗi xử lý AI: " + ex.Message, cancellationToken: stoppingToken);
            }
        }
    }
}
