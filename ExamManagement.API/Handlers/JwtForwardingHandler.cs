using Microsoft.AspNetCore.Http;
using System.Net.Http.Headers;

namespace ExamManagement.API.Handlers;

public class JwtForwardingHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public JwtForwardingHandler(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext != null && httpContext.Request.Headers.ContainsKey("Authorization"))
        {
            var authHeader = httpContext.Request.Headers["Authorization"].ToString();
            if (!string.IsNullOrEmpty(authHeader))
            {
                // CRITICAL: Always remove existing Authorization header first to avoid duplicates
                // This must be done before any attempt to set the header
                if (request.Headers.Contains("Authorization"))
                {
                    request.Headers.Remove("Authorization");
                }
                
                // Also clear the Authorization property if it's set
                request.Headers.Authorization = null;
                
                // Parse and set Authorization header using the proper property
                // This avoids the "multiple values" exception
                if (authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                {
                    var token = authHeader.Substring("Bearer ".Length).Trim();
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                }
                else if (authHeader.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
                {
                    var credentials = authHeader.Substring("Basic ".Length).Trim();
                    request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);
                }
                else
                {
                    // Fallback: if format is unknown, just use the raw header value
                    // But ensure we've cleared any existing header first
                    request.Headers.Authorization = null;
                    if (request.Headers.Contains("Authorization"))
                    {
                        request.Headers.Remove("Authorization");
                    }
                    // Use TryAddWithoutValidation as last resort
                    request.Headers.TryAddWithoutValidation("Authorization", authHeader);
                }
            }
        }

        return await base.SendAsync(request, cancellationToken);
    }
}

