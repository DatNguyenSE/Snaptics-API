using BLL.Dtos;
using BLL.Service;
using DAL.Entities;

namespace API.Extensions
{
    public static class AppUserExtensions
{
    public static async Task<UserDto> ToDto(this AppUser user, TokenService tokenService)
    {
        return new UserDto
        {
            Id = user.Id,
            DisplayName = user.DisplayName,
            Email = user.Email!,
            Token = await tokenService.CreateToken(user)
        };
    }
}
}