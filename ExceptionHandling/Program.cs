using ExceptionHandling.Demos;

namespace ExceptionHandling
{
    /// <summary>
    /// 异步异常处理示例程序
    /// 演示 AggregateException、Task.WhenAll 异常处理、SafeWhenAll 等核心概念
    /// </summary>
    internal class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("========================================");
            Console.WriteLine("异步异常处理示例");
            Console.WriteLine("========================================\n");

            while (true)
            {
                Console.WriteLine("\n请选择示例:");
                Console.WriteLine("1. await vs Wait/Result 异常行为对比");
                Console.WriteLine("2. Task.WhenAll 只抛出第一个异常的问题");
                Console.WriteLine("3. 解决方案1：手动检查 Task.Exception");
                Console.WriteLine("4. 解决方案2：逐个 await");
                Console.WriteLine("5. 解决方案3：SafeWhenAll 扩展方法");
                Console.WriteLine("6. 实战场景：并发调用 API + 容错处理");
                Console.WriteLine("7. 后台任务异常处理（Fire-and-Forget）");
                Console.WriteLine("8. AggregateException.Flatten() 用法");
                Console.WriteLine("9. AggregateException.Handle() 用法");
                Console.WriteLine("0. 退出");
                Console.Write("\n请输入选项: ");

                var input = Console.ReadLine();

                try
                {
                    switch (input)
                    {
                        case "1":
                            await BasicExceptionHandling.DemoAwaitVsWaitAsync();
                            break;

                        case "2":
                            await WhenAllExceptionHandling.DemoWhenAllFirstExceptionAsync();
                            break;

                        case "3":
                            await WhenAllExceptionHandling.DemoManualCheckExceptionAsync();
                            break;

                        case "4":
                            await WhenAllExceptionHandling.DemoAwaitIndividuallyAsync();
                            break;

                        case "5":
                            await SafeWhenAllDemo.DemoSafeWhenAllAsync();
                            break;

                        case "6":
                            await ApiAggregatorDemo.DemoApiAggregatorAsync();
                            break;

                        case "7":
                            await FireAndForgetDemo.DemoFireAndForgetAsync();
                            break;

                        case "8":
                            AggregateExceptionAdvanced.DemoFlatten();
                            break;

                        case "9":
                            AggregateExceptionAdvanced.DemoHandle();
                            break;

                        case "0":
                            Console.WriteLine("\n再见！");
                            return;

                        default:
                            Console.WriteLine("\n❌ 无效的选项，请重新输入。");
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"\n❌ 程序异常: {ex.GetType().Name}: {ex.Message}");
                }

                Console.WriteLine("\n按任意键继续...");
                Console.ReadKey();
                Console.Clear();
            }
        }
    }
}
