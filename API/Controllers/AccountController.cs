using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.Extensions;
using BLL.Dtos;
using BLL.Dtos.AccountDto;
using BLL.Dtos.EmailDto;
using BLL.Interfaces.IServices;
using DAL.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;


namespace API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AccountController(UserManager<AppUser> userManager, ITokenService tokenService, IMailService mailService) : ControllerBase
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

                // Generate OTP using Identity Token Provider
                var otp = await userManager.GenerateUserTokenAsync(newUser, TokenOptions.DefaultEmailProvider, "ConfirmEmail");

                // Send email
                try
                {
                    await mailService.SendEmailAsync(new EmailRequest
                    {
                        ToEmail = newUser.Email,
                        Subject = "Xác thực tài khoản Snaptics",
                        Body = $"Cảm ơn bạn đã đăng ký! Mã OTP xác thực tài khoản của bạn là: <strong>{otp}</strong>. Mã này có hiệu lực trong 5 phút."
                    });
                }
                catch (Exception)
                {
                    return Ok("Registration successful, but failed to send verification email. Please request a new OTP.");
                }
            }
            return Ok("Registration successful. Please check your email for the verification code.");
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

            if (!user.EmailConfirmed)
            {
                return BadRequest(new { emailConfirmed = false, message = "Tài khoản chưa được xác thực Email. Vui lòng xác thực trước khi đăng nhập." });
            }

            // Tạo Refresh Token và lưu vào Cookie
            await SetRefreshTokenCookie(user);
            var userDto = await user.ToDto(tokenService);
            return Ok(userDto);
        }

        [HttpPost("verify-otp")]
        public async Task<IActionResult> VerifyOtp(VerifyOtpDto dto)
        {
            var user = await userManager.FindByEmailAsync(dto.Email);
            if (user == null)
            {
                return BadRequest("Không tìm thấy tài khoản người dùng.");
            }

            if (user.EmailConfirmed)
            {
                return BadRequest("Tài khoản đã được xác thực trước đó.");
            }

            var isValid = await userManager.VerifyUserTokenAsync(user, TokenOptions.DefaultEmailProvider, "ConfirmEmail", dto.Otp);
            if (!isValid)
            {
                return BadRequest("Mã OTP không đúng hoặc đã hết hạn.");
            }

            user.EmailConfirmed = true;
            var updateResult = await userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                return BadRequest(updateResult.Errors);
            }

            return Ok("Xác thực tài khoản thành công. Bây giờ bạn có thể đăng nhập.");
        }

        [HttpPost("resend-otp")]
        public async Task<IActionResult> ResendOtp(ResendOtpDto dto)
        {
            var user = await userManager.FindByEmailAsync(dto.Email);
            if (user == null)
            {
                return BadRequest("Không tìm thấy tài khoản người dùng.");
            }

            if (user.EmailConfirmed)
            {
                return BadRequest("Tài khoản đã được xác thực.");
            }

            var otp = await userManager.GenerateUserTokenAsync(user, TokenOptions.DefaultEmailProvider, "ConfirmEmail");

            try
            {
                await mailService.SendEmailAsync(new EmailRequest
                {
                    ToEmail = user.Email!,
                    Subject = "Xác thực tài khoản Snaptics",
                    Body = $"Mã OTP xác thực tài khoản mới của bạn là: <strong>{otp}</strong>. Mã này có hiệu lực trong 5 phút."
                });
            }
            catch (Exception)
            {
                return BadRequest("Gửi email thất bại. Vui lòng thử lại sau.");
            }

            return Ok("Mã OTP mới đã được gửi tới email của bạn.");
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
