using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Repositories.Models;
using Services.Services;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace PRN231_SU25_SE183841.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly LeopardAccountService _leo;

        public AuthController(IConfiguration config, LeopardAccountService systemAccountService)
        {
            _config = config;
            _leo = systemAccountService;
        }

        public class LoginRequest
        {
            public string Email { get; set; }
            public string Password { get; set; }
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var user = await _leo.GetUserByCredentialsAsync(request.Email, request.Password);

            if (user == null)
                return StatusCode(401); // Không gửi body ở đây!

            var token = GenerateJSONWebToken(user);

            return Ok(new
            {
                token,
                role = Enum.IsDefined(typeof(RoleEnum), user.RoleId)
                    ? ((RoleEnum)user.RoleId).ToString()
                    : "Unknown"
            });
        }

        private string GenerateJSONWebToken(LeopardAccount systemUserAccount)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            // Map RoleId to enum name if possible
            string roleName = Enum.IsDefined(typeof(RoleEnum), systemUserAccount.RoleId)
                ? ((RoleEnum)systemUserAccount.RoleId).ToString()
                : "Unknown";

            var claims = new[]
            {
                new Claim(ClaimTypes.Name, systemUserAccount.FullName),
                new Claim(ClaimTypes.Email, systemUserAccount.Email),
                new Claim(ClaimTypes.Role, roleName)
            };

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(120),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
