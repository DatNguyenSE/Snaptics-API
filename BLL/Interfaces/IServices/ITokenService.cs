using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DAL.Entities;


namespace BLL.Interfaces.IServices
{
    public interface ITokenService
    {
        Task<string> CreateToken(AppUser user);
        string GenerateRefreshToken();
    }
}