using System;
using System.Security.Claims;
using System.Threading.Tasks;
using BLL.Dtos.UserProfiles;
using BLL.Interfaces.IServices;
using DAL.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/user/profile")]
    public class UserProfileController : ControllerBase
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly IS3Service _s3Service;

        public UserProfileController(UserManager<AppUser> userManager, IS3Service s3Service)
        {
            _userManager = userManager;
            _s3Service = s3Service;
        }

        [HttpGet]
        public async Task<IActionResult> GetProfile()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Unauthorized();

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            return Ok(new UserProfileDto
            {
                Id = user.Id,
                Email = user.Email,
                DisplayName = user.DisplayName,
                ImageUrl = user.ImageUrl,
                Address = user.Address,
                City = user.City,
                PostCode = user.PostCode,
                Country = user.Country,
                TrackCalories = user.TrackCalories,
                DefaultReminderTime = user.DefaultReminderTime
            });
        }

        [HttpPut]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Unauthorized();

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            user.DisplayName = dto.DisplayName ?? user.DisplayName;
            user.Address = dto.Address ?? user.Address;
            user.City = dto.City ?? user.City;
            user.PostCode = dto.PostCode ?? user.PostCode;
            user.Country = dto.Country ?? user.Country;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded) return BadRequest(result.Errors);

            return Ok("Profile updated successfully.");
        }

        [HttpPost("avatar")]
        public async Task<IActionResult> UploadAvatar(IFormFile file)
        {
            if (file == null || file.Length == 0) return BadRequest("File is empty.");
            if (file.Length > 20 * 1024 * 1024) return BadRequest("Max file size is 20MB."); // As per UI max 20mb

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Unauthorized();

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            var imageUrl = await _s3Service.UploadFileAsync(file, userId, "avatars");
            user.ImageUrl = imageUrl;
            
            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded) return BadRequest(result.Errors);

            return Ok(new { ImageUrl = imageUrl });
        }

        [HttpPut("email")]
        public async Task<IActionResult> ChangeEmail([FromBody] ChangeEmailDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Unauthorized();

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();
            
            var token = await _userManager.GenerateChangeEmailTokenAsync(user, dto.NewEmail);
            var result = await _userManager.ChangeEmailAsync(user, dto.NewEmail, token);
            
            if (!result.Succeeded) return BadRequest(result.Errors);
            
            await _userManager.SetUserNameAsync(user, dto.NewEmail);

            return Ok("Email changed successfully.");
        }

        [HttpPut("password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Unauthorized();

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            var result = await _userManager.ChangePasswordAsync(user, dto.CurrentPassword, dto.NewPassword);
            if (!result.Succeeded) return BadRequest(result.Errors);

            return Ok("Password changed successfully.");
        }
    }
}
