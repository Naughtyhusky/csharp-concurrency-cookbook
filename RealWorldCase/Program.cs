using RealWorldCase.Middleware;
using RealWorldCase.Services;

namespace RealWorldCase
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllers();
            builder.Services.AddOpenApi();

            // 注册文件处理演进演示服务（Singleton 以保持测试文件的生命周期一致）
            builder.Services.AddSingleton<FileProcessingEvolutions>();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
            }

            // ─── 中间件选择（四选一，演示不同境界）───
            // 取消下面其中一行的注释来体验不同版本的中间件：

            // 🥉 第一重：传统同步 Stream
            // app.UseSyncStreamLogging();

            // 🥈 第二重：异步 Stream + ArrayPool
            // app.UseAsyncStreamLogging();

            // 🥇 第三重：PipeReader 零拷贝
            app.UsePipeReaderLogging();

            // 👑 终极：流式 PipeReader（只读前4KB）
            // app.UseStreamingPipeReaderLogging();

            app.UseHttpsRedirection();

            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}
