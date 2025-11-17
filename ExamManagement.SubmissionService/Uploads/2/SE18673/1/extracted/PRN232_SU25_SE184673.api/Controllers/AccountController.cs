using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PRN232_SU25_SE184673.Service;

namespace PRN232_SU25_SE184673.api.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        private LeopardAccountService _accountService;
        private JwtService _jwtService;
        public AccountController(LeopardAccountService accountService, JwtService jwtService) 
        { 
            _accountService = accountService;
            _jwtService = jwtService;
        }

        [HttpPost]
        public async Task<IActionResult> Authentication(string email, string password)
        {
            if (string.IsNullOrEmpty(email)) return StatusCode(400, new
            {
                errorMessage = "Email must not null or empty"
            });
            if (string.IsNullOrEmpty(password)) return StatusCode(400, new
            {
                errorMessage = "Password must not null or empty"
            });
            var acc = await _accountService.Authentication(email, password);
            if (acc == null) return StatusCode(404);
            return Ok(_jwtService.GenerateToken(acc));
            
        }
    }
}
