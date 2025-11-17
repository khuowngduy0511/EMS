using ExamManagement.Common;
using ExamManagement.SemesterService.Data;
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
var semesterDbConnection = builder.Configuration.GetConnectionString("SemesterDB") 
    ?? builder.Configuration["ConnectionStrings:SemesterDB"] 
    ?? Constants.ConnectionStrings.SemesterDB;
builder.Services.AddDbContext<SemesterDbContext>(options =>
    options.UseSqlServer(semesterDbConnection));

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
app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "SemesterService", timestamp = DateTime.UtcNow }))
    .AllowAnonymous();

// Apply migrations and seed initial data
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<SemesterDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    
    try
    {
        context.Database.Migrate();
        logger.LogInformation("Database migrations applied successfully.");
        
        DbInitializer.Seed(context);
        logger.LogInformation("Database seeded successfully.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while migrating or seeding the database.");
    }
}

app.Run();
