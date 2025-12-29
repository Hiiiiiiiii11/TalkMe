
using ChatAppAPI.Jwt;
using CloudinaryDotNet;
using DotNetEnv;
using GrpcService;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Share.GrpcClient;
using Share.Services;
using SocialRepository.Data;
using SocialRepository.Repositories;
using SocialService.Services;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using UserService.Cloudinaries;

namespace SocialApi
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
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
                        o.UseHttps("/https/certs/chatapi.pfx", pfxPassword);
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

            builder.Services.AddDbContext<SocialDbContext>(options =>
            options.UseSqlServer(builder.Configuration.GetConnectionString("SocialDbConnection")));

            builder.Services.Configure<CloudinarySettings>(
            builder.Configuration.GetSection("CloudinarySettings"));

            builder.Services.AddSingleton(provider =>
            {
                var config = builder.Configuration.GetSection("CloudinarySettings").Get<CloudinarySettings>();
                var account = new Account(config.CloudName, config.ApiKey, config.ApiSecret);
                return new Cloudinary(account);
            });



            builder.Services.AddScoped<IMediaUploadService, MediaUploadService>();
            builder.Services.AddScoped<IPostRepository, PostRepository>();
            builder.Services.AddScoped<IPostMediaRepository, PostMediaRepository>();
            builder.Services.AddScoped<ILikeRepository, LikeRepository>();
            builder.Services.AddScoped<ICommentRepository, CommentRepository>();

            builder.Services.AddScoped<IPostService, PostService>();
            builder.Services.AddScoped<ILikeService, LikeService>();
            builder.Services.AddScoped<ICommentService, CommentService>();
            builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddHttpContextAccessor();

            builder.Services.AddControllers()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
                });
            builder.Services.AddEndpointsApiExplorer();

            // Thêm gRPC
            builder.Services.AddGrpc();

            var userServiceUrl = builder.Environment.IsProduction()
                ? "https://userapi:443"
                : "https://localhost:7216";

            var chatServiceUrl = builder.Environment.IsProduction()
                ? "https://conversation:443"
                : "https://localhost:7227";

            // Cập nhật cổng dev cho UserAPI
            var notificationServiceUrl = builder.Environment.IsProduction()
                ? "https://notificationapi:443"
                : "https://localhost:7292"; // Cập nhật cổng dev cho NotificationAPI

            if (builder.Environment.IsProduction())
            {
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

                builder.Services.AddGrpcClient<UserGrpcService.UserGrpcServiceClient>(o =>
                    o.Address = new Uri(userServiceUrl))
                    .ConfigurePrimaryHttpMessageHandler(() => handler);
                builder.Services.AddGrpcClient<ConversationGrpcService.ConversationGrpcServiceClient>(o =>
                 o.Address = new Uri(chatServiceUrl))
                    .ConfigurePrimaryHttpMessageHandler(() => handler);
                builder.Services.AddGrpcClient<MessageGrpcService.MessageGrpcServiceClient>(o =>
                 o.Address = new Uri(chatServiceUrl))
                    .ConfigurePrimaryHttpMessageHandler(() => handler);

                builder.Services.AddGrpcClient<NotificationGrpcService.NotificationGrpcServiceClient>(o =>
                    o.Address = new Uri(notificationServiceUrl))
                    .ConfigurePrimaryHttpMessageHandler(() => handler);
            }
            else // Cấu hình cho local dev
            {
                builder.Services.AddGrpcClient<UserGrpcService.UserGrpcServiceClient>(o =>
                   o.Address = new Uri(userServiceUrl));
                builder.Services.AddGrpcClient<ConversationGrpcService.ConversationGrpcServiceClient>(o =>
                   o.Address = new Uri(chatServiceUrl));
                builder.Services.AddGrpcClient<MessageGrpcService.MessageGrpcServiceClient>(o =>
                   o.Address = new Uri(chatServiceUrl));
                builder.Services.AddGrpcClient<NotificationGrpcService.NotificationGrpcServiceClient>(o =>
                    o.Address = new Uri(notificationServiceUrl));
            }
            builder.Services.AddScoped<IGrpcClient, GrpcClient>();
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
                if (builder.Environment.IsProduction())
                {
                    options.AddPolicy("AllowMainDomain", policy =>
                    {
                        policy.WithOrigins(
                                "https://fastchat1005.xyz",       // Domain chính (Swagger UI)
                                "https://www.fastchat1005.xyz"
                              )
                              .AllowAnyHeader()
                              .AllowAnyMethod();
                    });
                }

                // Policy này cho phép Domain chính gọi vào API này
                else
                {
                    options.AddPolicy("AllowAll", policy =>
                    {
                        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
                    });
                }

                // Policy cũ của bạn (Allow All)

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
            app.UseForwardedHeaders();
            if (builder.Environment.IsProduction())
            {
                app.UseCors("AllowMainDomain");
            }
            else
            {
                app.UseCors("AllowAll");
            }

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
                            var dbContext = services.GetRequiredService<SocialDbContext>();

                            var defaultConnStr = builder.Configuration.GetConnectionString("ChatDbConnection");
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

            // Configure the HTTP request pipeline.

            app.UseSwagger();
            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Production") || app.Environment.IsEnvironment("Docker"))
            {

                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}
