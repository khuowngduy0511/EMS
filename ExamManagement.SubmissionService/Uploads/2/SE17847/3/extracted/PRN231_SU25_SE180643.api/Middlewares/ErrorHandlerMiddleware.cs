using System.Text.Json;

namespace PRN231_SU25_SE180643.api.Middlewares
{
    public class ErrorHandlerMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ErrorHandlerMiddleware> _logger;

        public ErrorHandlerMiddleware(RequestDelegate next, ILogger<ErrorHandlerMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var originalBody = context.Response.Body;
            using var memStream = new MemoryStream();
            context.Response.Body = memStream;

            try
            {
                await _next(context);

                memStream.Seek(0, SeekOrigin.Begin);
                var bodyText = await new StreamReader(memStream).ReadToEndAsync();

                var statusCode = context.Response.StatusCode;
                var (errorCode, message) = GetErrorInfoFromStatus(statusCode);

                if (!string.IsNullOrEmpty(errorCode) && !context.Response.HasStarted)
                {
                    context.Response.Body = originalBody;
                    context.Response.ContentType = "application/json";
                    var error = new ErrorResponse
                    {
                        errorCode = errorCode,
                        message = message
                    };
                    var json = JsonSerializer.Serialize(error);
                    await context.Response.WriteAsync(json);
                }
                else
                {
                    memStream.Seek(0, SeekOrigin.Begin);
                    await memStream.CopyToAsync(originalBody);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception");
                context.Response.Body = originalBody;
                await HandleExceptionAsync(context, ex);
            }
        }

        private (string errorCode, string message) GetErrorInfoFromStatus(int statusCode)
        {
            return statusCode switch
            {
                400 => ("HB40001", "Missing/invalid input"),
                401 => ("HB40101", "Token missing/invalid"),
                403 => ("HB40301", "Permission denied"),
                404 => ("HB40401", "Resource not found"),
                500 => ("HB50001", "Internal server error"),
                _ => (null, null)
            };
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            if (context.Response.HasStarted) return;

            context.Response.Clear();
            context.Response.ContentType = "application/json";

            var statusCode = exception switch
            {
                UnauthorizedAccessException => 401,
                ArgumentException => 400,
                KeyNotFoundException => 404,
                _ => 500
            };

            var (errorCode, message) = GetErrorInfoFromStatus(statusCode);

            context.Response.StatusCode = statusCode;

            var error = new ErrorResponse
            {
                errorCode = errorCode ?? "HB50001",
                message = message ?? "Internal server error"
            };

            var json = JsonSerializer.Serialize(error);
            await context.Response.WriteAsync(json);
        }

        private class ErrorResponse
        {
            public string errorCode { get; set; }
            public string message { get; set; }
        }
    }
}
