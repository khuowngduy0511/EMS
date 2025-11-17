using ExamManagement.Common;
using ExamManagement.ViolationService.Data;
using ExamManagement.ViolationService.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
// Swagger disabled - only API Gateway should have Swagger
// builder.Services.AddSwaggerGen();

// JWT Authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = Constants.JwtIssuer,
        ValidAudience = Constants.JwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Constants.JwtSecretKey))
    };
});

builder.Services.AddAuthorization();

// Database - Read from configuration (environment variables) or fallback to Constants
var violationDbConnection = builder.Configuration.GetConnectionString("ViolationDB") 
    ?? builder.Configuration["ConnectionStrings:ViolationDB"] 
    ?? Constants.ConnectionStrings.ViolationDB;
builder.Services.AddDbContext<ViolationDbContext>(options =>
    options.UseSqlServer(violationDbConnection));

// Add HttpContextAccessor for JWT forwarding
builder.Services.AddHttpContextAccessor();

// Configure HttpClient for SubmissionService with JWT forwarding
var serviceUrls = builder.Configuration.GetSection("ServiceUrls");
builder.Services.AddHttpClient("SubmissionService", client =>
{
    var url = serviceUrls["SubmissionService"] ?? "http://localhost:5035";
    client.BaseAddress = new Uri(url);
    client.Timeout = TimeSpan.FromSeconds(30);
})
.AddHttpMessageHandler(serviceProvider =>
{
    return new ExamManagement.ViolationService.Handlers.JwtForwardingHandler(
        serviceProvider.GetRequiredService<IHttpContextAccessor>());
});

builder.Services.AddScoped<IViolationService, ViolationService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Only enable Swagger for API Gateway, not for individual services
// Comment Swagger for service con
// if (app.Environment.IsDevelopment() && app.Environment.ApplicationName.Contains("API"))
// {
//     app.UseSwagger();
//     app.UseSwaggerUI();
// }

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "ViolationService", timestamp = DateTime.UtcNow }))
    .AllowAnonymous();

// Apply migrations
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ViolationDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    
    try
    {
        context.Database.Migrate();
        logger.LogInformation("Database migrations applied successfully.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while migrating the database.");
    }
}

app.Run();
