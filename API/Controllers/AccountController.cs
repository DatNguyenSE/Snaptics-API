using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.Entities;
using BLL.Dtos.AccountDto;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [ApiController]
    [Route("api/account")]
    public class AccountController(UserManager<AppUser> userManager) : ControllerBase
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
            return Ok("Login successful");
        }



    }
}
