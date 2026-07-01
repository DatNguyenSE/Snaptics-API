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
    public class AccountController(UserManager<AppUser> userManager, ITokenService tokenService, IMailService mailService, IBudgetService _budgetService) : ControllerBase
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
                
                var userBudget = new BudgetDto
                {
                    UserId = newUser.Id,
                    StartDate = DateTime.Now,
                    CreatedAt = DateTime.Now,
                    IsActive = true
                };
                await _budgetService.CreateAsync(userBudget);

                // Generate OTP using Identity Token Provider
                var otp = await userManager.GenerateUserTokenAsync(newUser, TokenOptions.DefaultEmailProvider, "ConfirmEmail");

                // Send email
                try
                {
                    string emailBody = $@"
<div style='font-family: ""Segoe UI"", Tahoma, Geneva, Verdana, sans-serif; max-width: 600px; margin: 0 auto; padding: 30px; background-color: #ffffff; border-radius: 12px; box-shadow: 0 4px 15px rgba(0,0,0,0.05); border: 1px solid #f0f0f0;'>
    <div style='text-align: center; margin-bottom: 25px;'>
        <h1 style='color: #2563eb; margin: 0; font-size: 28px; font-weight: 700;'>Snaptics</h1>
    </div>
    <div style='color: #334155; font-size: 16px; line-height: 1.6;'>
        <p style='margin-top: 0;'>Chào bạn,</p>
        <p>Cảm ơn bạn đã đăng ký tài khoản tại <strong>Snaptics</strong>. Để hoàn tất quá trình đăng ký, vui lòng sử dụng mã xác thực (OTP) dưới đây:</p>
        <div style='background-color: #f8fafc; border: 1px solid #e2e8f0; border-radius: 8px; padding: 20px; text-align: center; margin: 30px 0;'>
            <span style='font-size: 36px; font-weight: 700; color: #0f172a; letter-spacing: 8px;'>{otp}</span>
        </div>
        <p>Mã này có hiệu lực trong <strong>5 phút</strong>.</p>
        <p style='color: #ef4444; font-size: 14px;'><i>Lưu ý: Tuyệt đối không chia sẻ mã này với bất kỳ ai để đảm bảo an toàn cho tài khoản của bạn.</i></p>
    </div>
    <hr style='border: none; border-top: 1px solid #e2e8f0; margin: 30px 0;' />
    <div style='text-align: center; color: #64748b; font-size: 13px;'>
        <p style='margin: 0;'>Nếu bạn không yêu cầu tạo tài khoản, xin vui lòng bỏ qua email này.</p>
        <p style='margin: 5px 0 0;'>&copy; {DateTime.Now.Year} Snaptics. All rights reserved.</p>
    </div>
</div>";

                    await mailService.SendEmailAsync(new EmailRequest
                    {
                        ToEmail = newUser.Email,
                        Subject = "Xác thực tài khoản Snaptics",
                        Body = emailBody
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
                return BadRequest(new { emailConfirmed = false, message = "The account's email has not been verified. Please verify it before logging in." });
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
                string emailBody = $@"
<div style='font-family: ""Segoe UI"", Tahoma, Geneva, Verdana, sans-serif; max-width: 600px; margin: 0 auto; padding: 30px; background-color: #ffffff; border-radius: 12px; box-shadow: 0 4px 15px rgba(0,0,0,0.05); border: 1px solid #f0f0f0;'>
    <div style='text-align: center; margin-bottom: 25px;'>
        <h1 style='color: #2563eb; margin: 0; font-size: 28px; font-weight: 700;'>Snaptics</h1>
    </div>
    <div style='color: #334155; font-size: 16px; line-height: 1.6;'>
        <p style='margin-top: 0;'>Chào bạn,</p>
        <p>Bạn đã yêu cầu gửi lại mã xác thực (OTP) cho tài khoản <strong>Snaptics</strong>. Vui lòng sử dụng mã dưới đây:</p>
        <div style='background-color: #f8fafc; border: 1px solid #e2e8f0; border-radius: 8px; padding: 20px; text-align: center; margin: 30px 0;'>
            <span style='font-size: 36px; font-weight: 700; color: #0f172a; letter-spacing: 8px;'>{otp}</span>
        </div>
        <p>Mã này có hiệu lực trong <strong>5 phút</strong>.</p>
        <p style='color: #ef4444; font-size: 14px;'><i>Lưu ý: Tuyệt đối không chia sẻ mã này với bất kỳ ai để đảm bảo an toàn cho tài khoản của bạn.</i></p>
    </div>
    <hr style='border: none; border-top: 1px solid #e2e8f0; margin: 30px 0;' />
    <div style='text-align: center; color: #64748b; font-size: 13px;'>
        <p style='margin: 0;'>Nếu bạn không yêu cầu mã này, xin vui lòng bảo mật tài khoản hoặc liên hệ với chúng tôi.</p>
        <p style='margin: 5px 0 0;'>&copy; {DateTime.Now.Year} Snaptics. All rights reserved.</p>
    </div>
</div>";

                await mailService.SendEmailAsync(new EmailRequest
                {
                    ToEmail = user.Email!,
                    Subject = "Xác thực tài khoản Snaptics",
                    Body = emailBody
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
