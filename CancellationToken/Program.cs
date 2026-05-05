namespace CancellationToken;

internal class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("=== CancellationToken 示例集合 ===\n");

        while (true)
        {
            Console.WriteLine("请选择示例：");
            Console.WriteLine("1. 基础用法：手动取消");
            Console.WriteLine("2. 超时自动取消");
            Console.WriteLine("3. 链接多个 CancellationToken");
            Console.WriteLine("4. 文件下载器（用户取消 + 超时）");
            Console.WriteLine("5. CPU 密集型任务取消");
            Console.WriteLine("6. 文件批量处理器");
            Console.WriteLine("7. 取消回调注册");
            Console.WriteLine("8. 错误示例：不传递 Token");
            Console.WriteLine("9. 性能基准测试（简单版）");
            Console.WriteLine("10. Token 的传递性（重要！）⭐");
            Console.WriteLine("11. 异步编程模式最佳实践 ⭐");
            Console.WriteLine("B. 性能基准测试（BenchmarkDotNet 完整版）");
            Console.WriteLine("0. 退出");
            Console.Write("\n请输入选择: ");

            var choice = Console.ReadLine()?.ToUpper();
            Console.WriteLine();

            try
            {
                switch (choice)
                {
                    case "1":
                        await Example01_BasicCancellation.RunAsync();
                        break;
                    case "2":
                        await Example02_TimeoutCancellation.RunAsync();
                        break;
                    case "3":
                        await Example03_LinkedTokens.RunAsync();
                        break;
                    case "4":
                        await Example04_FileDownloader.RunAsync();
                        break;
                    case "5":
                        await Example05_CpuIntensiveTask.RunAsync();
                        break;
                    case "6":
                        await Example06_FileBatchProcessor.RunAsync();
                        break;
                    case "7":
                        await Example07_CancellationCallback.RunAsync();
                        break;
                    case "8":
                        await Example08_BadExample.RunAsync();
                        break;
                    case "9":
                        CancurrencyCookBook.CancellationToken.CancellationPerformanceRunner.RunSimple();
                        break;
                    case "10":
                        await Example10_TokenPropagation.RunAsync();
                        break;
                    case "11":
                        await Example11_AsyncPatterns.RunAsync();
                        break;
                    case "B":
                        CancurrencyCookBook.CancellationToken.CancellationPerformanceRunner.Run();
                        break;
                    case "0":
                        Console.WriteLine("再见！");
                        return;
                    default:
                        Console.WriteLine("无效选择，请重试。");
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 发生错误: {ex.Message}");
            }

            Console.WriteLine("\n按任意键继续...\n");
            Console.ReadKey();
            Console.Clear();
        }
    }
}
