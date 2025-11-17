using Microsoft.AspNetCore.Http;
using System.Net.Http.Headers;
using System.Net.Http;

namespace ExamManagement.ViolationService.Handlers;

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
                // Always remove existing Authorization header first to avoid duplicates
                request.Headers.Remove("Authorization");
                request.Headers.Authorization = null;
                
                // Parse and set Authorization header using the proper property
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
                    request.Headers.Authorization = null;
                    if (request.Headers.Contains("Authorization"))
                    {
                        request.Headers.Remove("Authorization");
                    }
                    request.Headers.TryAddWithoutValidation("Authorization", authHeader);
                }
            }
        }

        return await base.SendAsync(request, cancellationToken);
    }
}

