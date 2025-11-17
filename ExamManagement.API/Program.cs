using ExamManagement.API.Hubs;
using ExamManagement.Common;

var builder = WebApplication.CreateBuilder(args);

// JWT Authentication for API Gateway
JwtHelper.ConfigureJwtAuthentication(builder.Services);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Exam Management API Gateway",
        Version = "v1",
        Description = "API Gateway for Exam Management Microservices"
    });
    c.IgnoreObsoleteActions();
    c.IgnoreObsoleteProperties();
    
    // Map IFormFile to binary schema
    c.MapType<Microsoft.AspNetCore.Http.IFormFile>(() => new Microsoft.OpenApi.Models.OpenApiSchema
    {
        Type = "string",
        Format = "binary"
    });
    
    // Add JWT Bearer authentication to Swagger
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token in the text input below.\n\nExample: \"Bearer 12345abcdef\"",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT"
    });
    
    // Add security requirement to all endpoints
    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
    
    // Add Operation Filter to create request body for file uploads
    // This filter handles [FromForm] parameters with IFormFile
    c.OperationFilter<ExamManagement.API.Swagger.FileUploadOperationFilter>();
});

// SignalR
builder.Services.AddSignalR();

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Configure HttpClient for microservices
var serviceUrls = builder.Configuration.GetSection("ServiceUrls");

void ConfigureHttpClient(string clientName, string defaultUrl, TimeSpan? timeout = null)
{
    builder.Services.AddHttpClient(clientName, client =>
    {
        var url = serviceUrls[clientName] ?? defaultUrl;
        try
        {
            client.BaseAddress = new Uri(url);
        }
        catch (UriFormatException)
        {
            // Fallback to default if URL is invalid
            client.BaseAddress = new Uri(defaultUrl);
        }
        client.Timeout = timeout ?? TimeSpan.FromSeconds(30);
    })
    .AddHttpMessageHandler(serviceProvider =>
    {
        return new ExamManagement.API.Handlers.JwtForwardingHandler(serviceProvider.GetRequiredService<IHttpContextAccessor>());
    });
}

// Add HttpContextAccessor for JWT forwarding
builder.Services.AddHttpContextAccessor();

ConfigureHttpClient("AuthService", "http://localhost:5001");
ConfigureHttpClient("SubjectService", "http://localhost:5004");
ConfigureHttpClient("SemesterService", "http://localhost:5245");
ConfigureHttpClient("ExamService", "http://localhost:5145");
ConfigureHttpClient("RubricService", "http://localhost:5078");
ConfigureHttpClient("ExaminerService", "http://localhost:5291");
ConfigureHttpClient("SubmissionService", "http://localhost:5035", TimeSpan.FromSeconds(60));
ConfigureHttpClient("ViolationService", "http://localhost:5005");
ConfigureHttpClient("ReportService", "http://localhost:5007");

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

// Swagger - Always enable for API Gateway (not for individual services)
// Individual services check: if (app.Environment.IsDevelopment() && app.Environment.ApplicationName.Contains("API"))
// API Gateway always shows Swagger because ApplicationName contains "API"
app.UseSwagger(c =>
{
    c.RouteTemplate = "swagger/{documentName}/swagger.json";
});
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Exam Management API Gateway v1");
    c.RoutePrefix = "swagger";
    c.ConfigObject.DisplayRequestDuration = true;
});

// Only use HTTPS redirection if HTTPS is configured
if (app.Configuration.GetValue<string>("ASPNETCORE_HTTPS_PORT") != null || 
    app.Configuration.GetValue<string>("ASPNETCORE_URLS")?.Contains("https") == true)
{
    app.UseHttpsRedirection();
}
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<ExamHub>("/examhub");

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "API Gateway", timestamp = DateTime.UtcNow }))
    .AllowAnonymous();

app.Run();
