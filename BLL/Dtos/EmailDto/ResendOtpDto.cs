using System.ComponentModel.DataAnnotations;

namespace BLL.Dtos.AccountDto
{
    public class ResendOtpDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
    }
}
