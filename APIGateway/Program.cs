namespace APIGateway
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // 1. Vẫn giữ SwaggerGen để nó chạy được UI
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();


            // 2. Sử dụng Swagger UI
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.RoutePrefix = string.Empty; // Hiển thị ở Root

                // --- QUAN TRỌNG: LIST CÁC SERVICE ---
                c.SwaggerEndpoint("https://localhost:7216/swagger/v1/swagger.json", "User Service");
                c.SwaggerEndpoint("https://localhost:7227/swagger/v1/swagger.json", "Chat Service");
                c.SwaggerEndpoint("https://localhost:7292/swagger/v1/swagger.json", "Notification Service");

                // Tắt validator để tránh lỗi vặt
                c.ConfigObject.AdditionalItems["validatorUrl"] = null;
            });

            // 3. [MẸO] Thêm một endpoint kiểm tra đơn giản
            // Nếu Swagger vẫn lỗi, ít nhất bạn truy cập localhost:port/health sẽ thấy chữ "OK"
            // để biết server Gateway đang sống.
            app.MapGet("/health", () => "Api Gateway is Running!");

            // 4. Redirect các request 404 về trang chủ (Optional)
            // Giúp lỡ tay gõ /swagger thì nó tự nhảy về /
            app.MapFallback(() => Results.Redirect("/"));

            app.Run();
        }
    }
}