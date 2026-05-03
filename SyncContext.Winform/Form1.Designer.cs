namespace SyncContext.Winform
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            btnDeadlock = new Button();
            btnCorrect = new Button();
            btnConfigureAwait = new Button();
            lblStatus = new Label();
            groupBox1 = new GroupBox();
            lblDescription = new Label();
            groupBox2 = new GroupBox();
            txtContextInfo = new TextBox();
            btnTestContext = new Button();
            groupBox1.SuspendLayout();
            groupBox2.SuspendLayout();
            SuspendLayout();
            // 
            // btnDeadlock
            // 
            btnDeadlock.BackColor = Color.FromArgb(255, 192, 192);
            btnDeadlock.Font = new Font("Microsoft YaHei UI", 12F);
            btnDeadlock.Location = new Point(16, 68);
            btnDeadlock.Margin = new Padding(2, 3, 2, 3);
            btnDeadlock.Name = "btnDeadlock";
            btnDeadlock.Size = new Size(171, 42);
            btnDeadlock.TabIndex = 0;
            btnDeadlock.Text = "💣 死锁示例 (.Result)";
            btnDeadlock.UseVisualStyleBackColor = false;
            btnDeadlock.Click += btnDeadlock_Click;
            // 
            // btnCorrect
            // 
            btnCorrect.BackColor = Color.FromArgb(192, 255, 192);
            btnCorrect.Font = new Font("Microsoft YaHei UI", 12F);
            btnCorrect.Location = new Point(202, 68);
            btnCorrect.Margin = new Padding(2, 3, 2, 3);
            btnCorrect.Name = "btnCorrect";
            btnCorrect.Size = new Size(171, 42);
            btnCorrect.TabIndex = 1;
            btnCorrect.Text = "✅ 正确示例 (async/await)";
            btnCorrect.UseVisualStyleBackColor = false;
            btnCorrect.Click += btnCorrect_Click;
            // 
            // btnConfigureAwait
            // 
            btnConfigureAwait.BackColor = Color.FromArgb(255, 255, 192);
            btnConfigureAwait.Font = new Font("Microsoft YaHei UI", 12F);
            btnConfigureAwait.Location = new Point(389, 68);
            btnConfigureAwait.Margin = new Padding(2, 3, 2, 3);
            btnConfigureAwait.Name = "btnConfigureAwait";
            btnConfigureAwait.Size = new Size(202, 42);
            btnConfigureAwait.TabIndex = 2;
            btnConfigureAwait.Text = "⚠️ ConfigureAwait 示例";
            btnConfigureAwait.UseVisualStyleBackColor = false;
            btnConfigureAwait.Click += btnConfigureAwait_Click;
            // 
            // lblStatus
            // 
            lblStatus.BackColor = SystemColors.Info;
            lblStatus.BorderStyle = BorderStyle.FixedSingle;
            lblStatus.Font = new Font("Consolas", 11F);
            lblStatus.Location = new Point(16, 128);
            lblStatus.Margin = new Padding(2, 0, 2, 0);
            lblStatus.Name = "lblStatus";
            lblStatus.Size = new Size(576, 68);
            lblStatus.TabIndex = 3;
            lblStatus.Text = "等待操作...";
            lblStatus.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // groupBox1
            // 
            groupBox1.Controls.Add(lblDescription);
            groupBox1.Font = new Font("Microsoft YaHei UI", 10F);
            groupBox1.Location = new Point(16, 212);
            groupBox1.Margin = new Padding(2, 3, 2, 3);
            groupBox1.Name = "groupBox1";
            groupBox1.Padding = new Padding(2, 3, 2, 3);
            groupBox1.Size = new Size(576, 102);
            groupBox1.TabIndex = 4;
            groupBox1.TabStop = false;
            groupBox1.Text = "说明";
            // 
            // lblDescription
            // 
            lblDescription.Dock = DockStyle.Fill;
            lblDescription.Location = new Point(2, 20);
            lblDescription.Margin = new Padding(2, 0, 2, 0);
            lblDescription.Name = "lblDescription";
            lblDescription.Padding = new Padding(8, 8, 8, 8);
            lblDescription.Size = new Size(572, 79);
            lblDescription.TabIndex = 0;
            lblDescription.Text = "🔴 红色按钮：调用 .Result 会导致死锁（程序无响应）\r\n\U0001f7e2 绿色按钮：正确的 async/await 用法\r\n\U0001f7e1 黄色按钮：使用 ConfigureAwait(false) 避免死锁";
            lblDescription.Click += lblDescription_Click;
            // 
            // groupBox2
            // 
            groupBox2.Controls.Add(txtContextInfo);
            groupBox2.Controls.Add(btnTestContext);
            groupBox2.Font = new Font("Microsoft YaHei UI", 10F);
            groupBox2.Location = new Point(16, 332);
            groupBox2.Margin = new Padding(2, 3, 2, 3);
            groupBox2.Name = "groupBox2";
            groupBox2.Padding = new Padding(2, 3, 2, 3);
            groupBox2.Size = new Size(576, 153);
            groupBox2.TabIndex = 5;
            groupBox2.TabStop = false;
            groupBox2.Text = "SynchronizationContext 测试";
            // 
            // txtContextInfo
            // 
            txtContextInfo.Font = new Font("Consolas", 9F);
            txtContextInfo.Location = new Point(16, 68);
            txtContextInfo.Margin = new Padding(2, 3, 2, 3);
            txtContextInfo.Multiline = true;
            txtContextInfo.Name = "txtContextInfo";
            txtContextInfo.ReadOnly = true;
            txtContextInfo.ScrollBars = ScrollBars.Vertical;
            txtContextInfo.Size = new Size(545, 73);
            txtContextInfo.TabIndex = 1;
            // 
            // btnTestContext
            // 
            btnTestContext.Location = new Point(16, 26);
            btnTestContext.Margin = new Padding(2, 3, 2, 3);
            btnTestContext.Name = "btnTestContext";
            btnTestContext.Size = new Size(156, 34);
            btnTestContext.TabIndex = 0;
            btnTestContext.Text = "🔍 检查上下文信息";
            btnTestContext.UseVisualStyleBackColor = true;
            btnTestContext.Click += btnTestContext_Click;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 17F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(610, 502);
            Controls.Add(groupBox2);
            Controls.Add(groupBox1);
            Controls.Add(lblStatus);
            Controls.Add(btnConfigureAwait);
            Controls.Add(btnCorrect);
            Controls.Add(btnDeadlock);
            Margin = new Padding(2, 3, 2, 3);
            Name = "Form1";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "SynchronizationContext 与死锁演示";
            groupBox1.ResumeLayout(false);
            groupBox2.ResumeLayout(false);
            groupBox2.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private Button btnDeadlock;
        private Button btnCorrect;
        private Button btnConfigureAwait;
        private Label lblStatus;
        private GroupBox groupBox1;
        private Label lblDescription;
        private GroupBox groupBox2;
        private Button btnTestContext;
        private TextBox txtContextInfo;
    }
}
