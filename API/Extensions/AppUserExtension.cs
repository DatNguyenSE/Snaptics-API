

using API.Entities;
using BLL.Dtos;
using BLL.Interfaces.IServices;

namespace API.Extension
{
    public static class AppUserExtensions
{
    public static async Task<UserDto> ToDto(this AppUser user, ITokenService tokenService)
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