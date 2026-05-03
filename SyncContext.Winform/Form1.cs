namespace SyncContext.Winform
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            lblStatus.Text = "就绪 - 请点击按钮测试";
        }

        // 💣 死锁按钮：同步等待异步方法
        private void btnDeadlock_Click(object sender, EventArgs e)
        {
            lblStatus.Text = "开始下载...（这会导致死锁！）";
            lblStatus.BackColor = Color.LightCoral;

            try
            {
                // ⚠️ 警告：这里会死锁！
                // UI 线程阻塞等待 Task 完成
                // 但 Task 完成后需要回到 UI 线程
                // UI 线程正在阻塞，无法处理请求
                // 结果：死锁
                string result = DownloadDataAsync().Result;

                lblStatus.Text = result;
                lblStatus.BackColor = SystemColors.Info;
            }
            catch (Exception ex)
            {
                lblStatus.Text = $"错误: {ex.Message}";
                lblStatus.BackColor = Color.LightPink;
            }
        }

        // ✅ 正确的异步按钮
        private async void btnCorrect_Click(object sender, EventArgs e)
        {
            lblStatus.Text = "开始下载...";
            lblStatus.BackColor = Color.LightYellow;

            try
            {
                // ✅ 正确做法：使用 await
                // UI 线程不会阻塞，可以继续处理其他消息
                // Task 完成后自动回到 UI 线程
                string result = await DownloadDataAsync();

                lblStatus.Text = result;
                lblStatus.BackColor = Color.LightGreen;
            }
            catch (Exception ex)
            {
                lblStatus.Text = $"错误: {ex.Message}";
                lblStatus.BackColor = Color.LightPink;
            }
        }

        // ⚠️ ConfigureAwait 按钮：避免死锁，但需要手动回到 UI 线程
        private void btnConfigureAwait_Click(object sender, EventArgs e)
        {
            lblStatus.Text = "开始下载...（使用 ConfigureAwait）";
            lblStatus.BackColor = Color.LightYellow;

            try
            {
                // ✅ 使用 ConfigureAwait(false) 避免死锁
                string result = DownloadDataWithConfigureAwaitAsync().Result;

                // ⚠️ 注意：这里可能不在 UI 线程！
                // 需要手动 Invoke 回到 UI 线程
                if (lblStatus.InvokeRequired)
                {
                    lblStatus.Invoke(new Action(() =>
                    {
                        lblStatus.Text = result;
                        lblStatus.BackColor = Color.LightGreen;
                    }));
                }
                else
                {
                    lblStatus.Text = result;
                    lblStatus.BackColor = Color.LightGreen;
                }
            }
            catch (Exception ex)
            {
                if (lblStatus.InvokeRequired)
                {
                    lblStatus.Invoke(new Action(() =>
                    {
                        lblStatus.Text = $"错误: {ex.Message}";
                        lblStatus.BackColor = Color.LightPink;
                    }));
                }
                else
                {
                    lblStatus.Text = $"错误: {ex.Message}";
                    lblStatus.BackColor = Color.LightPink;
                }
            }
        }

        // 🔍 测试 SynchronizationContext
        private async void btnTestContext_Click(object sender, EventArgs e)
        {
            var sb = new System.Text.StringBuilder();

            // 1. 当前线程信息
            sb.AppendLine($"1. 主线程 ID: {Environment.CurrentManagedThreadId}");
            sb.AppendLine($"   SynchronizationContext: {SynchronizationContext.Current?.GetType().Name ?? "null"}");
            sb.AppendLine();

            // 2. 在 Task.Run 中检查
            await Task.Run(() =>
            {
                sb.AppendLine($"2. Task.Run 线程 ID: {Environment.CurrentManagedThreadId}");
                sb.AppendLine($"   SynchronizationContext: {SynchronizationContext.Current?.GetType().Name ?? "null"}");
                sb.AppendLine();
            });

            // 3. await 之后
            sb.AppendLine($"3. await 之后线程 ID: {Environment.CurrentManagedThreadId}");
            sb.AppendLine($"   SynchronizationContext: {SynchronizationContext.Current?.GetType().Name ?? "null"}");
            sb.AppendLine();

            // 4. 测试 ConfigureAwait(false)
            int beforeConfigureAwait = Environment.CurrentManagedThreadId;
            await Task.Delay(100).ConfigureAwait(false);
            int afterConfigureAwait = Environment.CurrentManagedThreadId;

            sb.AppendLine($"4. ConfigureAwait(false) 测试:");
            sb.AppendLine($"   之前线程 ID: {beforeConfigureAwait}");
            sb.AppendLine($"   之后线程 ID: {afterConfigureAwait}");
            sb.AppendLine($"   (注意：线程 ID 可能不同)");

            // 需要回到 UI 线程更新控件
            if (txtContextInfo.InvokeRequired)
            {
                txtContextInfo.Invoke(new Action(() => txtContextInfo.Text = sb.ToString()));
            }
            else
            {
                txtContextInfo.Text = sb.ToString();
            }
        }

        // 模拟异步下载（默认行为）
        private static async Task<string> DownloadDataAsync()
        {
            int beforeAwait = Environment.CurrentManagedThreadId;
            var contextBefore = SynchronizationContext.Current?.GetType().Name ?? "null";

            // 模拟网络请求
            await Task.Delay(2000);

            int afterAwait = Environment.CurrentManagedThreadId;
            var contextAfter = SynchronizationContext.Current?.GetType().Name ?? "null";

            return $"✅ 下载完成！\n" +
                   $"await 之前:\n" +
                   $"  - 线程 ID: {beforeAwait}\n" +
                   $"  - Context: {contextBefore}\n" +
                   $"await 之后:\n" +
                   $"  - 线程 ID: {afterAwait}\n" +
                   $"  - Context: {contextAfter}\n" +
                   $"结论: 自动回到 UI 线程";
        }

        // 模拟异步下载（使用 ConfigureAwait(false)）
        private static async Task<string> DownloadDataWithConfigureAwaitAsync()
        {
            int beforeAwait = Environment.CurrentManagedThreadId;
            var contextBefore = SynchronizationContext.Current?.GetType().Name ?? "null";

            // 使用 ConfigureAwait(false) 避免捕获 SynchronizationContext
            await Task.Delay(2000).ConfigureAwait(false);

            int afterAwait = Environment.CurrentManagedThreadId;
            var contextAfter = SynchronizationContext.Current?.GetType().Name ?? "null";

            return $"✅ 下载完成！\n" +
                   $"await 之前:\n" +
                   $"  - 线程 ID: {beforeAwait}\n" +
                   $"  - Context: {contextBefore}\n" +
                   $"await 之后:\n" +
                   $"  - 线程 ID: {afterAwait}\n" +
                   $"  - Context: {contextAfter}\n" +
                   $"结论: 不回到 UI 线程（需要手动 Invoke）";
        }

        private void lblDescription_Click(object sender, EventArgs e)
        {

        }
    }
}
