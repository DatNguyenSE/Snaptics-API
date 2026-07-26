using System.Threading.Tasks;

namespace BLL.Interfaces.IServices
{
    public interface ISqsPublisherService
    {
        Task SendMessageAsync<T>(T message, string queueName = "snaptics-ai-queue");
    }
}
