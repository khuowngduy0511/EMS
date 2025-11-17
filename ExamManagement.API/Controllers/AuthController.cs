using ExamManagement.Contracts.Auth;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Json;

namespace ExamManagement.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IHttpClientFactory httpClientFactory, ILogger<AuthController> logger)
    {
        _httpClient = httpClientFactory.CreateClient("AuthService");
        _logger = logger;
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/auth/login", request);
            var content = await response.Content.ReadAsStringAsync();
            
            if (response.IsSuccessStatusCode)
            {
                var loginResponse = System.Text.Json.JsonSerializer.Deserialize<LoginResponse>(content, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                return Ok(loginResponse);
            }
            
            return StatusCode((int)response.StatusCode, content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling AuthService");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPost("refresh")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/auth/refresh", request);
            var content = await response.Content.ReadAsStringAsync();
            
            if (response.IsSuccessStatusCode)
            {
                var loginResponse = System.Text.Json.JsonSerializer.Deserialize<LoginResponse>(content, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                return Ok(loginResponse);
            }
            
            return StatusCode((int)response.StatusCode, content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling AuthService");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPost("revoke")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RevokeToken([FromBody] RefreshTokenRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/auth/revoke", request);
            var content = await response.Content.ReadAsStringAsync();
            
            if (response.IsSuccessStatusCode)
            {
                return Ok(new { message = "Token revoked successfully" });
            }
            
            return StatusCode((int)response.StatusCode, content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling AuthService");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPost("invite")]
    [ProducesResponseType(typeof(InviteUserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> InviteUser([FromBody] InviteUserRequest request)
    {
        try
        {
            // JWT token is automatically forwarded by JwtForwardingHandler
            var requestMessage = new HttpRequestMessage(HttpMethod.Post, "/api/auth/invite")
            {
                Content = System.Net.Http.Json.JsonContent.Create(request)
            };
            
            var response = await _httpClient.SendAsync(requestMessage);
            var content = await response.Content.ReadAsStringAsync();
            
            if (response.IsSuccessStatusCode)
            {
                var inviteResponse = System.Text.Json.JsonSerializer.Deserialize<InviteUserResponse>(content, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                return Ok(inviteResponse);
            }
            
            return StatusCode((int)response.StatusCode, content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling AuthService to invite user");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet("accept-invite")]
    [ProducesResponseType(typeof(ValidateInviteTokenResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> ValidateInviteToken([FromQuery] string token)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/auth/accept-invite?token={Uri.EscapeDataString(token)}");
            var content = await response.Content.ReadAsStringAsync();
            
            if (response.IsSuccessStatusCode)
            {
                var validateResponse = System.Text.Json.JsonSerializer.Deserialize<ValidateInviteTokenResponse>(content, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                return Ok(validateResponse);
            }
            
            return StatusCode((int)response.StatusCode, content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling AuthService to validate invite token");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPost("accept-invite")]
    [ProducesResponseType(typeof(AcceptInviteResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> AcceptInvite([FromBody] AcceptInviteRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/auth/accept-invite", request);
            var content = await response.Content.ReadAsStringAsync();
            
            if (response.IsSuccessStatusCode)
            {
                var acceptResponse = System.Text.Json.JsonSerializer.Deserialize<AcceptInviteResponse>(content, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                return Ok(acceptResponse);
            }
            
            return StatusCode((int)response.StatusCode, content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling AuthService to accept invitation");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet("users")]
    [ProducesResponseType(typeof(List<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetUsers()
    {
        try
        {
            // JWT token is automatically forwarded by JwtForwardingHandler
            var requestMessage = new HttpRequestMessage(HttpMethod.Get, "/api/auth/users");
            
            var response = await _httpClient.SendAsync(requestMessage);
            var content = await response.Content.ReadAsStringAsync();
            
            if (response.IsSuccessStatusCode)
            {
                var users = System.Text.Json.JsonSerializer.Deserialize<List<UserDto>>(content, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                return Ok(users);
            }
            
            return StatusCode((int)response.StatusCode, content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling AuthService to get users");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet("users/{id}")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUser(int id)
    {
        try
        {
            // JWT token is automatically forwarded by JwtForwardingHandler
            var requestMessage = new HttpRequestMessage(HttpMethod.Get, $"/api/auth/users/{id}");
            
            var response = await _httpClient.SendAsync(requestMessage);
            var content = await response.Content.ReadAsStringAsync();
            
            if (response.IsSuccessStatusCode)
            {
                var user = System.Text.Json.JsonSerializer.Deserialize<UserDto>(content, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                return Ok(user);
            }
            
            return StatusCode((int)response.StatusCode, content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling AuthService to get user {Id}", id);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpDelete("users/{id}")]
    [ProducesResponseType(typeof(DeleteUserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeleteUser(int id)
    {
        try
        {
            // JWT token is automatically forwarded by JwtForwardingHandler
            var requestMessage = new HttpRequestMessage(HttpMethod.Delete, $"/api/auth/users/{id}");
            
            var response = await _httpClient.SendAsync(requestMessage);
            var content = await response.Content.ReadAsStringAsync();
            
            if (response.IsSuccessStatusCode)
            {
                var deleteResponse = System.Text.Json.JsonSerializer.Deserialize<DeleteUserResponse>(content, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                return Ok(deleteResponse);
            }
            
            return StatusCode((int)response.StatusCode, content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling AuthService to delete user {Id}", id);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }
}

