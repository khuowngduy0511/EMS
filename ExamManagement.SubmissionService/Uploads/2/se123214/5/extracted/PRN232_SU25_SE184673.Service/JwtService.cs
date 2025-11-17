using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using PRN232_SU25_SE184673.Repository.DTO;
using PRN232_SU25_SE184673.Repository.Models;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace PRN232_SU25_SE184673.Service
{
    public class JwtService
    {
        private readonly IConfiguration _config;

        public JwtService(IConfiguration config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config), "Configuration cannot be null");
        }

        public LoginResponse? GenerateToken(LeopardAccount account)
        {
            var jwtSection = _config.GetSection("Jwt");
            var key = Encoding.UTF8.GetBytes(jwtSection["Key"]);
            var creds = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256
                );
            var claim = new List<Claim>
            {
                new Claim(ClaimTypes.Name, account.FullName),
                new Claim(ClaimTypes.Email, account.Email),
                new Claim(ClaimTypes.Role, account.RoleId.ToString())
            };
            var token = new JwtSecurityToken(
                issuer: jwtSection["Issuer"],
                audience: jwtSection["Audience"],
                claims: claim,
                expires: DateTime.Now.AddMinutes(Convert.ToDouble(jwtSection["DurationMinutes"])),
                signingCredentials: creds
            );
            string role = "";
            switch (account.RoleId)
            {
                case 5:
                    role = "administrator";
                    break;
                case 6:
                    role = "moderator";
                    break;
                case 7:
                    role = "developer";
                    break;
                case 4:
                    role = "member";
                    break;
                case 1:
                case 2:
                case 3:
                    break;
            }
            if (role.Equals("")) return null;
            return new LoginResponse
            {
                Token = new JwtSecurityTokenHandler().WriteToken(token),
                Role = role
            };
        }
    }
}
