using ChatAppAPI.Jwt;
using CloudinaryDotNet;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Share.Services;
using System.Text;
using UserRepository.Admin;
using UserRepository.Data;
using UserRepository.Models;
using UserRepository.Repositories;
using UserRepository.VerifyEmail;
using UserService.Cloudinaries;
using UserService.Repositories;
using UserService.Services;
// Thêm namespace này để dùng SqlException
using Microsoft.Data.SqlClient;

namespace ChatAppAPI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // --- PHẦN NÀY GIỮ NGUYÊN ---
            builder.Services.AddDbContext<UserDbContext>(options =>
            options.UseSqlServer(builder.Configuration.GetConnectionString("UserDbConnection")));

            builder.Services.Configure<CloudinarySettings>(
            builder.Configuration.GetSection("CloudinarySettings"));
            builder.Services.Configure<AdminAccountSettings>(
            builder.Configuration.GetSection("AdminAccountSettings"));
            builder.Services.Configure<EmailSettings>(
            builder.Configuration.GetSection("EmailSettings"));

            builder.Services.AddScoped<EmailSettings>(sp =>
                sp.GetRequiredService<IOptions<EmailSettings>>().Value);
            builder.Services.AddSingleton(sp =>
            sp.GetRequiredService<IOptions<AdminAccountSettings>>().Value);

            builder.Services.AddSingleton(provider =>
            {
                var config = builder.Configuration.GetSection("CloudinarySettings").Get<CloudinarySettings>();
                var account = new Account(config.CloudName, config.ApiKey, config.ApiSecret);
                return new Cloudinary(account);
            });

            builder.Services.AddScoped<IUserRepository, UserRepository.Repositories.UserRepository>();
            builder.Services.AddScoped<IUserService, UserService.Services.UserService>();
            builder.Services.AddScoped<IAuthenticationRepository, AuthenticationRepository>();
            builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
            builder.Services.AddScoped<IUploadPhotoService, UploadPhotoService>();
            builder.Services.AddScoped<IEmailVerificationRepository, EmailVerificationRepository>();
            builder.Services.AddScoped<IEmailVerificationService, EmailVerificationService>();
            builder.Services.AddScoped<IPasswordResetTokenRepository, PasswordResetTokenRepository>();
            builder.Services.AddScoped<IPasswordResetService, PasswordResetService>();
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddGrpc();

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
                            var dbContext = services.GetRequiredService<UserDbContext>();

                            // Bước 1: Tự tạo DB nếu chưa có
                            var defaultConnStr = builder.Configuration.GetConnectionString("UserDbConnection");
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
                                Console.WriteLine($"✅ Step 1/3: Database '{dbName}' created or already exists.");
                            }

                            // Bước 2: Tạo schema (các bảng)
                            dbContext.Database.EnsureCreated();
                            Console.WriteLine("✅ Step 2/3: Schema has been created successfully.");

                            // Bước 3: Seed admin account
                            var adminSettings = services.GetRequiredService<IOptions<AdminAccountSettings>>().Value;
                            if (!dbContext.Users.Any(u => u.Email == adminSettings.Email))
                            {
                                var hashedPassword = BCrypt.Net.BCrypt.HashPassword(adminSettings.Password);
                                var adminUser = new User
                                {
                                    Id = Guid.NewGuid(),
                                    Email = adminSettings.Email,
                                    PasswordHash = hashedPassword,
                                    DisplayName = adminSettings.DisplayName,
                                    IsActive = true
                                };
                                dbContext.Users.Add(adminUser);
                                dbContext.SaveChanges();
                                Console.WriteLine("✅ Step 3/3: Admin account has been seeded successfully.");
                            }

                            break; // Thoát vòng lặp nếu tất cả thành công
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

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Production") || app.Environment.IsEnvironment("Docker"))
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
