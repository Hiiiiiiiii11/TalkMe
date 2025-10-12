using ChatAppAPI.Jwt;
using GrpcService;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using NotificationRepository.Data;
using NotificationRepository.Repositories;
using NotificationService.Implement;
using NotificationService.Services;
using System.Text;
// Thêm namespace này để dùng SqlException
using Microsoft.Data.SqlClient;

namespace NotificationApi
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.Services.AddDbContext<NotificationDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("NotificationDbConnection")));
            // Add services to the container.
            builder.Services.AddScoped<INotificationRepository, NotificationRepository.Repositories.NotificationRepository>();
            builder.Services.AddScoped<INotificationService, NotificationService.Services.NotificationService>();

            builder.Services.AddControllers();
            builder.Services.AddControllers()
              .AddJsonOptions(options =>
              {
                  options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
              });
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
                {
                    Title = "ChatApp API",
                    Version = "v1",
                    Description = "API for Chat Application"
                });
                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "Enter JWT."
                });
                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        Array.Empty<string>()
                    }
                });
            });
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", policyBuilder =>
                {
                    policyBuilder.AllowAnyOrigin()
                                 .AllowAnyMethod()
                                 .AllowAnyHeader();
                });
            });
            builder.Services.AddGrpc();
            var grpcSettings = builder.Configuration.GetSection("GrpcServices");

            string GetGrpcUrl(string key)
            {
                var envKey = key.Replace("Api", "").ToUpper() + "_URL"; // USERAPI_URL, CHATAPI_URL, NOTIFICATIONAPI_URL
                var url = Environment.GetEnvironmentVariable(envKey);
                if (!string.IsNullOrEmpty(url))
                    return url;

                var cfg = grpcSettings[key];
                if (string.IsNullOrEmpty(cfg))
                    throw new Exception($"GrpcService URL for {key} not configured");
                return cfg;
            }

            builder.Services.AddGrpcClient<MessageGrpcService.MessageGrpcServiceClient>(o =>
            {
                o.Address = new Uri(GetGrpcUrl("ChatApi"));
            });

            builder.Services.AddGrpcClient<ConversationGrpcService.ConversationGrpcServiceClient>(o =>
            {
                o.Address = new Uri(GetGrpcUrl("ChatApi"));
            });

            builder.Services.AddGrpcClient<UserGrpcService.UserGrpcServiceClient>(o =>
            {
                o.Address = new Uri(GetGrpcUrl("UserApi"));
            });

            builder.Services.Configure<JwtSettings>(
            builder.Configuration.GetSection("Jwt")
            );

            var jwtSettings = builder.Configuration.GetSection("Jwt").Get<JwtSettings>();
            var key = Encoding.UTF8.GetBytes(jwtSettings.SecretKey);

            builder.Services
                .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.RequireHttpsMetadata = false;
                    options.SaveToken = true;
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = jwtSettings.Issuer,
                        ValidAudience = jwtSettings.Audience,
                        IssuerSigningKey = new SymmetricSecurityKey(key)
                    };
                    options.Events = new JwtBearerEvents
                    {
                        OnChallenge = async context =>
                        {
                            context.HandleResponse();
                            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                            context.Response.ContentType = "application/json";
                            await context.Response.WriteAsync(
                                "{\"message\":\"Unauthorized - Token is missing or invalid.\"}");
                        },
                        OnForbidden = async context =>
                        {
                            context.Response.StatusCode = StatusCodes.Status403Forbidden;
                            context.Response.ContentType = "application/json";
                            await context.Response.WriteAsync(
                                "{\"message\":\"Forbidden - You do not have permission to access this resource.\"}");
                        },
                    };

                });

            var app = builder.Build();

            // =================================================================
            // === THAY ĐỔI QUAN TRỌNG: THÊM VÒNG LẶP RETRY KHI MIGRATE DB ===
            // =================================================================
            if (app.Environment.IsEnvironment("Production") || app.Environment.IsEnvironment("Docker"))
            {
                int maxRetries = 10;
                int delayInSeconds = 5;

                for (int i = 0; i < maxRetries; i++)
                {
                    using (var scope = app.Services.CreateScope())
                    {
                        var services = scope.ServiceProvider;
                        try
                        {
                            var dbContext = services.GetRequiredService<NotificationDbContext>();
                            dbContext.Database.Migrate();
                            Console.WriteLine("✅ Database has been migrated successfully.");
                            break; // Thoát vòng lặp nếu thành công
                        }
                        catch (SqlException ex)
                        {
                            Console.WriteLine($"❌ Attempt {i + 1} of {maxRetries}: Database is not ready yet. Retrying in {delayInSeconds} seconds... Error: {ex.Message}");
                            Thread.Sleep(TimeSpan.FromSeconds(delayInSeconds));
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"❌ An unexpected error occurred while migrating the database: {ex.Message}");
                            break;
                        }
                    }
                }
            }
            // =================================================================
            // === KẾT THÚC THAY ĐỔI ===
            // =================================================================

            app.MapGrpcService<NotificationGrpcServiceImpl>();

            if (app.Environment.IsDevelopment() || app.Environment.IsProduction() || app.Environment.IsEnvironment("Docker"))
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }
            app.UseCors("AllowAll");
            app.UseHttpsRedirection();
            app.UseAuthentication();
            app.UseAuthorization();
            app.MapControllers();
            app.Run();
        }
    }
}
