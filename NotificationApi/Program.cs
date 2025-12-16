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
using Microsoft.AspNetCore.Server.Kestrel.Core;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.HttpOverrides;
using DotNetEnv;

namespace NotificationApi
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.Services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            });

            if (builder.Environment.IsProduction())
            {
                var pfxPassword = builder.Configuration["Kestrel:CertificatePassword"];
                builder.WebHost.ConfigureKestrel(options =>
                {
                    options.ListenAnyIP(8080, o => o.Protocols = HttpProtocols.Http1);
                    options.ListenAnyIP(443, o =>
                    {
                        o.Protocols = HttpProtocols.Http2;
                        o.UseHttps("/https/certs/notificationapi.pfx", pfxPassword);
                    });
                });
            }

            if (builder.Environment.IsDevelopment())
            {
                Env.TraversePath().Load(".env.Local");
            }

            // --- THÊM DÒNG NÀY ---
            // Dòng này giúp .NET đọc các biến vừa load từ file .env vào builder.Configuration
            builder.Configuration.AddEnvironmentVariables();

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
                // Policy này cho phép Domain chính gọi vào API này
                options.AddPolicy("AllowMainDomain", policy =>
                {
                    policy.WithOrigins(
                            "https://fastchat1005.xyz",       // Domain chính (Swagger UI)
                            "https://www.fastchat1005.xyz"
                          )
                          .AllowAnyHeader()
                          .AllowAnyMethod();
                });

                // Policy cũ của bạn (Allow All)
                options.AddPolicy("AllowAll", policy =>
                {
                    policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
                });
            });
            builder.Services.AddGrpc();
            var userServiceUrl = builder.Environment.IsProduction()
                ? "https://userapi:443"
                : "https://localhost:7216"; // Cổng dev của UserAPI

            var chatServiceUrl = builder.Environment.IsProduction()
                ? "https://chatapi:443"
                : "https://localhost:7227"; // << THAY THẾ cổng dev của ChatAPI tại đây nếu cần

            // 2. Tách biệt cấu hình client cho từng môi trường
            if (builder.Environment.IsProduction())
            {
                // --- Cấu hình cho PRODUCTION (Docker) ---
                var handler = new HttpClientHandler();
                var caCert = new X509Certificate2("/https/certs/ca.crt");
                handler.ServerCertificateCustomValidationCallback = (message, serverCert, chain, errors) =>
                {
                    if (serverCert == null) return false;
                    using var customChain = new X509Chain();
                    customChain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
                    customChain.ChainPolicy.CustomTrustStore.Add(caCert);
                    customChain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
                    return customChain.Build(serverCert);
                };

                // Đăng ký các client với handler tùy chỉnh
                builder.Services.AddGrpcClient<UserGrpcService.UserGrpcServiceClient>(o =>
                    o.Address = new Uri(userServiceUrl))
                    .ConfigurePrimaryHttpMessageHandler(() => handler);
                builder.Services.AddGrpcClient<MessageGrpcService.MessageGrpcServiceClient>(o =>
                    o.Address = new Uri(chatServiceUrl))
                    .ConfigurePrimaryHttpMessageHandler(() => handler);
                builder.Services.AddGrpcClient<ConversationGrpcService.ConversationGrpcServiceClient>(o =>
                    o.Address = new Uri(chatServiceUrl))
                    .ConfigurePrimaryHttpMessageHandler(() => handler);
            }
            else
            {
                // --- Cấu hình cho LOCAL DEVELOPMENT ---
                builder.Services.AddGrpcClient<UserGrpcService.UserGrpcServiceClient>(o =>
                    o.Address = new Uri(userServiceUrl));
                builder.Services.AddGrpcClient<MessageGrpcService.MessageGrpcServiceClient>(o =>
                    o.Address = new Uri(chatServiceUrl));
                builder.Services.AddGrpcClient<ConversationGrpcService.ConversationGrpcServiceClient>(o =>
                    o.Address = new Uri(chatServiceUrl));
            }

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
            app.UseCors("AllowAll");

            // =================================================================
            // === ÁP DỤNG LOGIC TẠO DATABASE MẠNH MẼ TỪ DỰ ÁN CŨ CỦA BẠN ===
            // =================================================================
            if (app.Environment.IsEnvironment("Production") || app.Environment.IsEnvironment("Docker"))
            {
                int maxRetries = 10;
                int delayInSeconds = 5;

                for (int i = 0; i < maxRetries; i++)
                {
                    try
                    {
                        using (var scope = app.Services.CreateScope())
                        {
                            var services = scope.ServiceProvider;
                            var dbContext = services.GetRequiredService<NotificationDbContext>();

                            var defaultConnStr = builder.Configuration.GetConnectionString("NotificationDbConnection");
                            var dbName = new SqlConnectionStringBuilder(defaultConnStr).InitialCatalog;
                            var masterConnStr = defaultConnStr.Replace($"Database={dbName}", "Database=master");

                            using (var connection = new SqlConnection(masterConnStr))
                            {
                                connection.Open();
                                using (var command = connection.CreateCommand())
                                {
                                    command.CommandText = $"IF DB_ID('{dbName}') IS NULL CREATE DATABASE {dbName}";
                                    command.ExecuteNonQuery();
                                }
                            }

                            dbContext.Database.EnsureCreated();
                            Console.WriteLine($"✅ Database '{dbName}' and schema have been created successfully.");

                            break;
                        }
                    }
                    catch (SqlException ex)
                    {
                        Console.WriteLine($"❌ Attempt {i + 1} of {maxRetries}: Database is not ready yet. Retrying in {delayInSeconds} seconds... Error: {ex.Message}");
                        Thread.Sleep(TimeSpan.FromSeconds(delayInSeconds));
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"❌ An unexpected error occurred: {ex.Message}");
                        break;
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
            
            app.UseAuthentication();
            app.UseAuthorization();
            app.MapControllers();
            app.Run();
        }
    }
}
