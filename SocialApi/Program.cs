
using ChatAppAPI.Jwt;
using CloudinaryDotNet;
using DotNetEnv;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Share.Services;
using SocialRepository.Data;
using SocialRepository.Repositories;
using SocialService.Services;
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

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();


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

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}
