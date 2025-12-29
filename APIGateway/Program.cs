using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace APIGateway
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            app.UseSwagger();

            // Cấu hình Swagger UI hiển thị danh sách API động theo môi trường
            app.UseSwaggerUI(c =>
            {
                c.RoutePrefix = string.Empty; // Chạy ngay tại trang chủ (root)

                // LOGIC QUAN TRỌNG: Kiểm tra môi trường để chọn URL đúng
                if (app.Environment.IsDevelopment())
                {
                    // --- MÔI TRƯỜNG LOCAL (Chạy máy tính cá nhân) ---
                    c.SwaggerEndpoint("https://localhost:7216/swagger/v1/swagger.json", "User Service (Local)");
                    c.SwaggerEndpoint("https://localhost:7227/swagger/v1/swagger.json", "Chat Service (Local)");
                    c.SwaggerEndpoint("https://localhost:7292/swagger/v1/swagger.json", "Notification Service (Local)");
                    c.SwaggerEndpoint("https://localhost:7255/swagger/v1/swagger.json", "Social Service (Local)");

                }
                if (app.Environment.IsProduction())
                {
                    // --- MÔI TRƯỜNG PRODUCTION (Chạy trên Server/Docker) ---
                    // Trỏ thẳng vào Domain public (HTTPS)
                    c.SwaggerEndpoint("https://user.fastchat1005.xyz/swagger/v1/swagger.json", "User Service");
                    c.SwaggerEndpoint("https://chat.fastchat1005.xyz/swagger/v1/swagger.json", "Chat Service");
                    c.SwaggerEndpoint("https://notification.fastchat1005.xyz/swagger/v1/swagger.json", "Notification Service");
                }

                // Tắt validator để tránh lỗi vặt trên UI
                c.ConfigObject.AdditionalItems["validatorUrl"] = null;

                // Giữ trạng thái expand (mở rộng) của các tag
                c.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.List);
                c.EnablePersistAuthorization();
            });

            // Endpoint health check đơn giản
            app.MapGet("/health", () => "APIGateway is running!");

            // Fallback: Nếu gõ link linh tinh thì quay về trang chủ (Swagger UI)
            app.MapFallback(() => Results.Redirect("/"));

            app.Run();
        }
    }
}