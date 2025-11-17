using ExamManagement.Common;
using ExamManagement.ReportService.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.OData;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OData.Edm;
using Microsoft.OData.ModelBuilder;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddOData(options => options
        .Select()
        .Filter()
        .OrderBy()
        .SetMaxTop(100)
        .AddRouteComponents("odata", GetEdmModel()));

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
var reportDbConnection = builder.Configuration.GetConnectionString("ReportDB") 
    ?? builder.Configuration["ConnectionStrings:ReportDB"] 
    ?? Constants.ConnectionStrings.ReportDB;
builder.Services.AddDbContext<ReportDbContext>(options =>
    options.UseSqlServer(reportDbConnection));

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
    return new ExamManagement.ReportService.Handlers.JwtForwardingHandler(
        serviceProvider.GetRequiredService<IHttpContextAccessor>());
});

// Configure HttpClient for ViolationService with JWT forwarding
builder.Services.AddHttpClient("ViolationService", client =>
{
    var url = serviceUrls["ViolationService"] ?? "http://localhost:5005";
    client.BaseAddress = new Uri(url);
    client.Timeout = TimeSpan.FromSeconds(30);
})
.AddHttpMessageHandler(serviceProvider =>
{
    return new ExamManagement.ReportService.Handlers.JwtForwardingHandler(
        serviceProvider.GetRequiredService<IHttpContextAccessor>());
});

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
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "ReportService", timestamp = DateTime.UtcNow }))
    .AllowAnonymous();

// Apply migrations
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ReportDbContext>();
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

static IEdmModel GetEdmModel()
{
    var builder = new ODataConventionModelBuilder();
    builder.EntitySet<ExamManagement.Models.Report.Report>("Reports");
    return builder.GetEdmModel();
}
