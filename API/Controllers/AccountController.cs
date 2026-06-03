using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.Entities;
using API.Extension;
using API.Extensions;
using BLL.Dtos;
using BLL.Dtos.AccountDto;
using BLL.Interfaces.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;


namespace API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AccountController(UserManager<AppUser> userManager, ITokenService tokenService) : ControllerBase
    {
        [HttpPost("register")] 
        public async Task<IActionResult> Register(RegisterDto dto)
        {
            var user = await userManager.FindByEmailAsync(dto.Email);
            if(user != null)
            {
                return BadRequest("Email already in use");
            }
            else
            {
                var newUser = new AppUser
                {
                    DisplayName = dto.DisplayName,
                    Email = dto.Email,
                    UserName = dto.Email
                };
                var result = await userManager.CreateAsync(newUser, dto.Password);
                if(!result.Succeeded)
                {
                    return BadRequest(result.Errors);
                }
                
                 await userManager.AddToRoleAsync(newUser, "user");
            }
            return Ok("Registration successful");
        } 

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDto dto)
        {
            var user = await userManager.FindByEmailAsync(dto.Email);
            if(user == null)
            {
                return Unauthorized("Invalid email or password");
            }
            var result = await userManager.CheckPasswordAsync(user, dto.Password);
            if(!result)
            {
                return Unauthorized("Invalid email or password");
            }

            // Tạo Refresh Token và lưu vào Cookie
            await SetRefreshTokenCookie(user);
            var userDto = await user.ToDto(tokenService);
            return Ok(userDto);
        }

        [Authorize]
        [HttpPost("logout")]
        public async Task<ActionResult> Logout()
        {
            await userManager.Users
                .Where(x => x.Id == User.GetUserId())
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(x => x.RefreshToken, _ => null)
                    .SetProperty(x => x.RefreshTokenExpiry, _ => null)
                    );

            Response.Cookies.Delete("refreshToken");

            return Ok();
        }


        [HttpPost("refresh-token")]
        public async Task<ActionResult<UserDto>> RefreshToken()
        {
            var refreshToken = Request.Cookies["refreshToken"];
            if (refreshToken == null) return NoContent();

            var user = await userManager.Users
                .FirstOrDefaultAsync(x => x.RefreshToken == refreshToken
                    && x.RefreshTokenExpiry > DateTime.UtcNow);

            if (user == null) return Unauthorized("Your login has expired, please log in again.");

            await SetRefreshTokenCookie(user);

            return await user.ToDto(tokenService);
        }

         private async Task SetRefreshTokenCookie(AppUser user)
        {
            var refreshToken = tokenService.GenerateRefreshToken();
            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7);
            await userManager.UpdateAsync(user);

            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,                 // JavaScript không đọc trộm được (Chống XSS)
                Secure = true,                   // Chỉ chạy trên HTTPS
                SameSite = SameSiteMode.None,  // Chỉ gửi cookie khi request từ chính trang web của bạn (Chống CSRF)
                Expires = DateTime.UtcNow.AddDays(7)
            };

            Response.Cookies.Append("refreshToken", refreshToken, cookieOptions);
        }
    }
}
