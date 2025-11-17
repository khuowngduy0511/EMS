using ExamManagement.Common;
using ExamManagement.SubjectService.Data;
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
var subjectDbConnection = builder.Configuration.GetConnectionString("SubjectDB") 
    ?? builder.Configuration["ConnectionStrings:SubjectDB"] 
    ?? Constants.ConnectionStrings.SubjectDB;

builder.Services.AddDbContext<SubjectDbContext>(options =>
    options.UseSqlServer(subjectDbConnection, sqlOptions =>
    {
        sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorNumbersToAdd: null);
    }));

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

// Exception handling middleware - must be first
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/error");
}

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
app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "SubjectService", timestamp = DateTime.UtcNow }))
    .AllowAnonymous();

// Apply migrations and seed initial data - don't block startup if this fails
try
{
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<SubjectDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    
        logger.LogInformation("Initializing database...");
        
    try
    {
            // Check if database can be connected
            if (!context.Database.CanConnect())
            {
                logger.LogWarning("Cannot connect to database. Service will start but database operations may fail.");
                logger.LogWarning("Please ensure SQL Server is running and the database exists.");
                
                // Mask password in connection string for logging
                var maskedConnection = string.IsNullOrEmpty(subjectDbConnection) 
                    ? "Not configured" 
                    : System.Text.RegularExpressions.Regex.Replace(
                        subjectDbConnection, 
                        @"Password\s*=\s*[^;]+", 
                        "Password=***",
                        System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                logger.LogWarning("Connection string: {ConnectionString}", maskedConnection);
            }
            else
            {
                logger.LogInformation("Database connection successful.");
                
                // Check for pending migrations
                var pendingMigrations = context.Database.GetPendingMigrations().ToList();
                if (pendingMigrations.Any())
                {
                    logger.LogInformation("Applying {Count} pending migrations...", pendingMigrations.Count);
        context.Database.Migrate();
        logger.LogInformation("Database migrations applied successfully.");
                }
                else
                {
                    logger.LogInformation("Database is up to date. No migrations to apply.");
                }
        
                // Seed initial data
                logger.LogInformation("Seeding database...");
        DbInitializer.Seed(context);
        logger.LogInformation("Database seeded successfully.");
            }
    }
    catch (Exception ex)
    {
            logger.LogError(ex, "An error occurred while initializing the database.");
            logger.LogError("Service will continue running, but database operations may fail.");
            // Don't throw - allow service to continue
        }
    }
}
catch (Exception ex)
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogError(ex, "Failed to create service scope for database initialization.");
    // Continue - service should still start
}

app.Run();
