using System.Threading.Tasks;
using BLL.Dtos.EmailDto;

namespace BLL.Interfaces.IServices
{
    public interface IMailService
    {
        Task SendEmailAsync(EmailRequest request);
    }
}
