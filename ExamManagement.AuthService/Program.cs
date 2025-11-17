using ExamManagement.AuthService.Data;
using ExamManagement.AuthService.Services;
using ExamManagement.Common;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
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
var authDbConnection = builder.Configuration.GetConnectionString("AuthDB") 
    ?? builder.Configuration["ConnectionStrings:AuthDB"] 
    ?? Constants.ConnectionStrings.AuthDB;
builder.Services.AddDbContext<AuthDbContext>(options =>
    options.UseSqlServer(authDbConnection));

// Services
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IAuthService, AuthService>();

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

var app = builder.Build();

// Configure the HTTP request pipeline
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
app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "AuthService", timestamp = DateTime.UtcNow }))
    .AllowAnonymous();

// Apply migrations and seed initial data
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    
    try
    {
        // Ensure database exists
        if (context.Database.GetPendingMigrations().Any())
        {
            logger.LogInformation("Applying database migrations...");
            context.Database.Migrate();
            logger.LogInformation("Database migrations applied successfully.");
        }
        else
        {
            logger.LogInformation("Database is up to date. No migrations to apply.");
        }

        // Ensure database can be connected
        logger.LogInformation("Checking database connection...");
        if (!context.Database.CanConnect())
        {
            logger.LogError("Cannot connect to database. Please check connection string and ensure SQL Server is running.");
            throw new InvalidOperationException("Cannot connect to database.");
        }
        logger.LogInformation("Database connection successful.");

        // Seed initial data for all roles
        logger.LogInformation("Seeding database with users for all roles (Admin, Manager, Moderator, Examiner)...");
        try
        {
            DbInitializer.Seed(context);
            logger.LogInformation("Database seeded successfully.");
        }
        catch (Exception seedEx)
        {
            logger.LogError(seedEx, "Error during database seeding.");
            throw;
        }
        
        // Verify seeded data
        var userCount = context.Users.Count();
        logger.LogInformation($"Total users in database: {userCount}");
        if (userCount == 0)
        {
            logger.LogWarning("WARNING: No users found in database after seeding!");
        }
        else
        {
            var roles = context.Users.Select(u => u.Role).Distinct().OrderBy(r => r).ToList();
            logger.LogInformation($"Roles in database: {string.Join(", ", roles)}");
            
            // Log each user
            var allUsers = context.Users.ToList();
            foreach (var user in allUsers)
            {
                logger.LogInformation($"  User: {user.Username}, Role: {user.Role}, Active: {user.IsActive}");
            }
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while setting up the database.");
        throw; // Re-throw to prevent app from starting with invalid database
    }
}

app.Run();
