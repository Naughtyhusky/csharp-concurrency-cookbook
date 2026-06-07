using Dataflow.Production;

namespace Dataflow;

/// <summary>
/// TPL Dataflow 示例程序入口
/// 演示流水线编程的核心概念和实战应用
/// </summary>
internal class Program
{
    static async Task Main(string[] args)
    {
        Console.Clear();
        Console.WriteLine("╔════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║                                                            ║");
        Console.WriteLine("║   TPL Dataflow 流水线编程示例                              ║");
        Console.WriteLine("║   ============================================              ║");
        Console.WriteLine("║                                                            ║");
        Console.WriteLine("║   1. 基本块（ActionBlock, TransformBlock 等）              ║");
        Console.WriteLine("║   2. LinkTo 数据传递（路由、扇出/扇入）                    ║");
        Console.WriteLine("║   3. 背压控制（BoundedCapacity）                           ║");
        Console.WriteLine("║   4. 实战：图片处理流水线                                  ║");
        Console.WriteLine("║                                                            ║");
        Console.WriteLine("║   ────────────────────────────────────────                ║");
        Console.WriteLine("║   🏭 生产级示例（推荐）：                                  ║");
        Console.WriteLine("║   5. 生产级图片处理流水线（完整实现）                      ║");
        Console.WriteLine("║   6. 性能对比（串行 vs 并行 vs 流水线）                    ║");
        Console.WriteLine("║   7. 容错测试（部分失败场景）                              ║");
        Console.WriteLine("║   8. 背压演示（快速生产 vs 慢速消费）                      ║");
        Console.WriteLine("║                                                            ║");
        Console.WriteLine("║   0. 运行所有示例                                          ║");
        Console.WriteLine("║                                                            ║");
        Console.WriteLine("╚════════════════════════════════════════════════════════════╝\n");

        while (true)
        {
            Console.Write("请选择示例（输入数字，按 Q 退出）: ");
            var input = Console.ReadLine()?.Trim().ToUpper();

            if (input == "Q")
            {
                Console.WriteLine("\n感谢使用！欢迎访问 GitHub 查看完整代码：");
                Console.WriteLine("https://github.com/Naughtyhusky/csharp-concurrency-cookbook\n");
                break;
            }

            try
            {
                Console.WriteLine();

                switch (input)
                {
                    case "1":
                        await BasicBlocksDemo.RunAllDemos();
                        break;

                    case "2":
                        await LinkToDemo.RunAllDemos();
                        break;

                    case "3":
                        await BackpressureDemo.RunAllDemos();
                        break;

                    case "4":
                        await ImagePipelineDemo.RunAllDemos();
                        break;

                    case "5":
                        await ProductionPipelineDemo.RunProductionPipelineDemo();
                        break;

                    case "6":
                        await ProductionPipelineDemo.RunComparisonDemo();
                        break;

                    case "7":
                        await ProductionPipelineDemo.RunFaultToleranceDemo();
                        break;

                    case "8":
                        await ProductionPipelineDemo.RunBackpressureDemo();
                        break;

                    case "0":
                        Console.WriteLine("▶ 运行所有示例（这可能需要几分钟）...\n");
                        await RunAllDemos();
                        Console.WriteLine("\n✅ 所有示例运行完成！\n");
                        break;

                    default:
                        Console.WriteLine("❌ 无效选择，请重新输入\n");
                        continue;
                }

                Console.WriteLine("\n" + new string('─', 60) + "\n");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n❌ 运行出错: {ex.Message}\n");
            }
        }
    }

    /// <summary>
    /// 运行所有示例
    /// </summary>
    private static async Task RunAllDemos()
    {
        var demos = new (string Name, Func<Task> Demo)[]
        {
            ("1️⃣  基本块示例", BasicBlocksDemo.RunAllDemos),
            ("2️⃣  LinkTo 数据传递", LinkToDemo.RunAllDemos),
            ("3️⃣  背压控制", BackpressureDemo.RunAllDemos),
            ("4️⃣  图片处理流水线", ImagePipelineDemo.RunAllDemos)
        };

        for (int i = 0; i < demos.Length; i++)
        {
            var (name, demo) = demos[i];

            Console.WriteLine($"\n╔════════════════════════════════════════════════════════════╗");
            Console.WriteLine($"║   {name,-54} ║");
            Console.WriteLine($"╚════════════════════════════════════════════════════════════╝\n");

            await demo();

            if (i < demos.Length - 1)
            {
                Console.WriteLine("\n⏸  暂停 2 秒...\n");
                await Task.Delay(2000);
            }
        }
    }
}
