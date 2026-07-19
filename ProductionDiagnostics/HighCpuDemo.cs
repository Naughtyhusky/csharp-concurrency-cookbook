namespace ProductionDiagnostics;

/// <summary>
/// CPU 占用过高演示：模拟导致 CPU 占用 100% 的场景
/// 诊断工具：dotnet-trace、PerfView
/// </summary>
public static class HighCpuDemo
{
    public static void Run()
    {
        Console.WriteLine("\n=== CPU 占用过高演示 ===");
        Console.WriteLine("将模拟几种导致 CPU 占用过高的场景\n");
        Console.WriteLine("⚠️  建议使用以下工具诊断：");
        Console.WriteLine("1. dotnet-trace collect -p <进程ID> --duration 00:00:60");
        Console.WriteLine("2. PerfView → Collect → CPU Samples");
        Console.WriteLine("3. Visual Studio → 调试 → 性能分析器 → CPU 使用率\n");

        Console.WriteLine("请选择要演示的场景：");
        Console.WriteLine("1. 死循环（忘记 await）");
        Console.WriteLine("2. 低效算法（N平方复杂度）");
        Console.WriteLine("3. 频繁的字符串拼接");
        Console.WriteLine("4. 过度的反射调用");
        Console.WriteLine("0. 返回\n");

        Console.Write("请输入选项: ");
        var choice = Console.ReadLine();

        switch (choice)
        {
            case "1":
                DemoInfiniteLoop();
                break;
            case "2":
                DemoInefficientAlgorithm();
                break;
            case "3":
                DemoStringConcatenation();
                break;
            case "4":
                DemoReflection();
                break;
            case "0":
                return;
            default:
                Console.WriteLine("无效的选项");
                break;
        }
    }

    private static void DemoInfiniteLoop()
    {
        Console.WriteLine("\n--- 场景1：死循环（忘记 await）---\n");
        Console.WriteLine("❌ 错误代码：");
        Console.WriteLine(@"
public async Task ProcessAsync()
{
    while (true)
    {
        var data = await GetDataAsync();
        Task.Delay(1000); // ❌ 忘记 await，导致死循环！
    }
}");

        Console.WriteLine("\n即将启动一个死循环（按 Ctrl+C 终止）...");
        Console.WriteLine("按任意键开始...");
        Console.ReadKey();

        Console.WriteLine("\n死循环已启动，观察 CPU 占用率（应该接近 100%）");
        Console.WriteLine("5 秒后自动停止...\n");

        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var iteration = 0;

        try
        {
            while (!cts.Token.IsCancellationRequested)
            {
                // ❌ 错误：没有 await，导致死循环
                Task.Delay(1000); // 应该是 await Task.Delay(1000)
                iteration++;

                if (iteration % 100000 == 0)
                {
                    Console.WriteLine($"迭代次数: {iteration}");
                }
            }
        }
        catch (OperationCanceledException)
        {
            // 取消
        }

        Console.WriteLine($"\n死循环已停止，总迭代次数: {iteration:N0}");
        Console.WriteLine("\n✅ 正确做法：");
        Console.WriteLine("await Task.Delay(1000); // 添加 await");
    }

    private static void DemoInefficientAlgorithm()
    {
        Console.WriteLine("\n--- 场景2：低效算法（O(n²) 复杂度）---\n");
        Console.WriteLine("比较冒泡排序和内置排序的性能差异\n");

        var sizes = new[] { 1000, 5000, 10000 };

        foreach (var size in sizes)
        {
            Console.WriteLine($"数组大小: {size}");

            // 生成随机数组
            var random = new Random(42);
            var array1 = Enumerable.Range(0, size).Select(_ => random.Next(10000)).ToArray();
            var array2 = (int[])array1.Clone();

            // 冒泡排序（O(n²)）
            var sw = System.Diagnostics.Stopwatch.StartNew();
            BubbleSort(array1);
            sw.Stop();
            Console.WriteLine($"  冒泡排序: {sw.ElapsedMilliseconds}ms");

            // 内置排序（O(n log n)）
            sw.Restart();
            Array.Sort(array2);
            sw.Stop();
            Console.WriteLine($"  内置排序: {sw.ElapsedMilliseconds}ms");
            Console.WriteLine();
        }

        Console.WriteLine("✅ 结论：");
        Console.WriteLine("- 冒泡排序的时间复杂度是 O(n²)，数据量大时性能很差");
        Console.WriteLine("- 内置排序使用快速排序/归并排序，复杂度是 O(n log n)");
        Console.WriteLine("- 选择合适的算法对性能至关重要");
    }

    private static void BubbleSort(int[] array)
    {
        int n = array.Length;
        for (int i = 0; i < n - 1; i++)
        {
            for (int j = 0; j < n - i - 1; j++)
            {
                if (array[j] > array[j + 1])
                {
                    (array[j], array[j + 1]) = (array[j + 1], array[j]);
                }
            }
        }
    }

    private static void DemoStringConcatenation()
    {
        Console.WriteLine("\n--- 场景3：频繁的字符串拼接 ---\n");
        Console.WriteLine("比较 string 拼接和 StringBuilder 的性能差异\n");

        var iterations = 10000;

        // 方式1：string 拼接（低效）
        var sw = System.Diagnostics.Stopwatch.StartNew();
        string result1 = "";
        for (int i = 0; i < iterations; i++)
        {
            result1 += $"Item {i}|"; // ❌ 每次拼接都创建新字符串
        }
        sw.Stop();
        Console.WriteLine($"string 拼接 ({iterations} 次): {sw.ElapsedMilliseconds}ms");
        Console.WriteLine($"最终字符串长度: {result1.Length}");

        // 方式2：StringBuilder（高效）
        sw.Restart();
        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < iterations; i++)
        {
            sb.Append($"Item {i}|"); // ✅ 复用内部缓冲区
        }
        string result2 = sb.ToString();
        sw.Stop();
        Console.WriteLine($"\nStringBuilder ({iterations} 次): {sw.ElapsedMilliseconds}ms");
        Console.WriteLine($"最终字符串长度: {result2.Length}");

        Console.WriteLine("\n✅ 结论：");
        Console.WriteLine("- string 是不可变类型，每次拼接都创建新对象");
        Console.WriteLine("- StringBuilder 使用可变缓冲区，避免频繁分配");
        Console.WriteLine("- 循环中拼接字符串，始终使用 StringBuilder");
    }

    private static void DemoReflection()
    {
        Console.WriteLine("\n--- 场景4：过度的反射调用 ---\n");
        Console.WriteLine("比较反射调用和直接调用的性能差异\n");

        var iterations = 1000000;
        var testObject = new TestClass();

        // 方式1：直接调用
        var sw = System.Diagnostics.Stopwatch.StartNew();
        for (int i = 0; i < iterations; i++)
        {
            testObject.Add(i, i + 1);
        }
        sw.Stop();
        Console.WriteLine($"直接调用 ({iterations:N0} 次): {sw.ElapsedMilliseconds}ms");

        // 方式2：反射调用
        var methodInfo = typeof(TestClass).GetMethod(nameof(TestClass.Add))!;
        sw.Restart();
        for (int i = 0; i < iterations; i++)
        {
            methodInfo.Invoke(testObject, new object[] { i, i + 1 }); // ❌ 每次都装箱
        }
        sw.Stop();
        Console.WriteLine($"反射调用 ({iterations:N0} 次): {sw.ElapsedMilliseconds}ms");

        // 方式3：缓存委托（优化反射）
        var addDelegate = (Func<int, int, int>)Delegate.CreateDelegate(
            typeof(Func<int, int, int>), testObject, methodInfo);
        sw.Restart();
        for (int i = 0; i < iterations; i++)
        {
            addDelegate(i, i + 1); // ✅ 缓存委托，避免反射开销
        }
        sw.Stop();
        Console.WriteLine($"委托调用 ({iterations:N0} 次): {sw.ElapsedMilliseconds}ms");

        Console.WriteLine("\n✅ 结论：");
        Console.WriteLine("- 反射调用比直接调用慢 50-100 倍");
        Console.WriteLine("- 如果必须使用反射，缓存 MethodInfo 和委托");
        Console.WriteLine("- 考虑使用表达式树编译委托（Expression.Compile）");
        Console.WriteLine("- 或使用源生成器（Source Generator）在编译时生成代码");
    }

    private class TestClass
    {
        public int Add(int a, int b) => a + b;
    }
}
