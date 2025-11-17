using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Repositories.Models;
using Services.IServices;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace PRN231_SU25_SE180643.api.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly ILeopardAccountService _leopardAccountService;

        public AuthController(IConfiguration config, ILeopardAccountService systemAccountService)
        {
            _config = config;
            _leopardAccountService = systemAccountService;
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
            var user = await _leopardAccountService.GetUserByCredentialsAsync(request.Email, request.Password);

            if (user == null)
                return StatusCode(401); // Không gửi body ở đây!

            var token = GenerateJSONWebToken(user);

            return Ok(new
            {
                token,
                role = user.RoleId.ToString(),
            });
        }

        private string GenerateJSONWebToken(LeopardAccount account)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            // Map RoleId to enum name if possible
            string roleName = Enum.IsDefined(typeof(RoleEnum), account.RoleId)
                ? ((RoleEnum)account.RoleId).ToString()
                : "Unknown";

            var claims = new[]
            {
                new Claim(ClaimTypes.Name, account.UserName),
                new Claim(ClaimTypes.Email, account.Email),
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
