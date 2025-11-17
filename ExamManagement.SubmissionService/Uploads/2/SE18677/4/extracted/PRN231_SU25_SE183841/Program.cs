
using Microsoft.AspNetCore.OData;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OData.Edm;
using Microsoft.OData.ModelBuilder;
using Microsoft.OpenApi.Models;
using PRN231_SU25_SE183841.Midddleware;
using Repositories.Interface;
using Repositories.Models;
using Repositories.Repositories;
using Services.Services;
using System.Text;

namespace PRN231_SU25_SE183841
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddDbContext<SU25LeopardDBContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

            builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer("Bearer", options =>
    {
        var jwtConfig = builder.Configuration.GetSection("Jwt");
        options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtConfig["Issuer"],
            ValidAudience = jwtConfig["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtConfig["Key"]))
        };
    });

            builder.Services.AddAuthorization();
            builder.Services.AddControllers()
                .AddOData(options =>
                {
                    options.Select().Filter().Count().OrderBy().Expand()  // Cấu hình OData
                           .SetMaxTop(100)
                           .AddRouteComponents("odata", GetEdmModel());  // Thêm route OData với EDM Model
                });


            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "PRN232 API", Version = "v1" });

                // Cấu hình cho JWT Bearer
                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = "JWT Authorization header. Nhập vào dạng: Bearer {token}",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.Http,
                    Scheme = "Bearer",
                    BearerFormat = "JWT"
                });

                c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Id = "Bearer",
                    Type = ReferenceType.SecurityScheme
                }
            },
            Array.Empty<string>()
        }
    });
            });
            builder.Services.AddScoped<ILeopardAccountRepo, LeopardAccountRepo>();
            builder.Services.AddScoped<LeopardAccountService>();
            builder.Services.AddScoped<ILeopardProfileRepo, LeopardProfileRepo>();
            builder.Services.AddScoped< LeopardProfileService>();
            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();
            app.UseMiddleware<ErrorHandlerMiddleware>();

            app.UseAuthorization();


            app.MapControllers();

            app.Run();
            IEdmModel GetEdmModel()
            {
                var builder = new ODataConventionModelBuilder();
                //builder.EntitySet<Brand>("Brands");  // Thêm EntitySet cho Book
                //builder.EntitySet<Handbag>("Handbags");  // Thêm EntitySet cho Press
                return builder.GetEdmModel();
            }
        }

    }
}
