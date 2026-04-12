# 贡献指南

感谢您对本项目的关注！本文档说明如何为项目贡献代码和文档。

---

## 📋 项目维护规范

### 🔄 添加新章节时的 README 更新流程

本项目是系列教程（共 21 章），为了保持 README 简洁且易维护，请遵循以下规范：

#### 1. 项目结构部分

**只需更新**：
```markdown
├── Overview/                   # 第一章：并发编程全景图
├── Threads/                    # 第二章：线程的底层原理  ← 新增
├── (更多章节代码将陆续添加...)
```

**不需要**列出每个文件的详细信息（避免维护负担）

#### 2. 当前可用内容部分

**格式**：
```markdown
### ✅ 第 X 章：章节名称

**学习目标**：简短描述（1-2 句话）

**代码位置**：`FolderName/` 文件夹

| 文件 | 说明 | 核心内容 |
|------|------|---------|
| `FileA.cs` | 简短描述 | 1-2 个关键点 |
| `FileB.cs` | 简短描述 | 1-2 个关键点 |

**运行方式**：
\`\`\`bash
dotnet run --project FolderName
\`\`\`
```

#### 3. 路线图部分

**更新表格**：
```markdown
| ✅ 第 X 章 | **已完成** | 章节名称 | `FolderName/` |
```

**更新进度**：
```markdown
### 📊 整体进度：X/21 章节（Y%）
```

#### 4. 代码示例部分

**原则**：
- ✅ 只展示**最有代表性**的 1-3 个示例
- ✅ 代码片段控制在 20 行以内
- ❌ 不要把所有代码都放进 README

---

## 🎯 代码贡献规范

### 文件组织

每个章节独立一个文件夹：

```
ChapterName/
├── Program.cs              # 入口文件
├── Demo1.cs               # 示例 1
├── Demo2.cs               # 示例 2
└── README.md              # 章节说明（可选）
```

### 代码规范

1. **注释要求**：
   ```csharp
   /// <summary>
   /// 清晰的方法说明
   /// </summary>
   private static void DemoMethod()
   {
       // 关键步骤的注释
       var result = DoSomething();
       
       // 解释为什么这样做
       Console.WriteLine($"结果: {result}");
   }
   ```

2. **命名规范**：
   - 文件名：`PascalCase`（如 `AsyncDemo.cs`）
   - 类名：`PascalCase`（如 `AsyncDemo`）
   - 方法名：`PascalCase`（如 `RunAsync`）
   - 变量名：`camelCase`（如 `threadId`）

3. **示例代码要求**：
   - ✅ 必须能够编译运行
   - ✅ 必须有清晰的输出
   - ✅ 必须有注释说明核心概念
   - ❌ 避免过于复杂的依赖

---

## 📝 文档贡献规范

### README.md 维护原则

1. **保持简洁**：
   - 避免过度详细的文件列表
   - 只展示核心概念和代码片段
   - 使用表格和徽章提高可读性

2. **易于维护**：
   - 新章节只需更新 3-4 处
   - 不要硬编码具体文件名（除非必要）
   - 使用相对路径

3. **可扩展性**：
   - 预留未来章节的空间
   - 使用 "..." 表示更多内容
   - 路线图清晰标记状态

### 章节内 README（可选）

如果章节比较复杂，可以创建独立的 `ChapterName/README.md`：

```markdown
# 第 X 章：章节名称

## 学习目标
...

## 代码列表
- `Demo1.cs`: 示例 1 说明
- `Demo2.cs`: 示例 2 说明

## 运行方式
\`\`\`bash
dotnet run --project ChapterName
\`\`\`

## 核心要点
1. 要点 1
2. 要点 2

## 参考资料
- [Microsoft Docs](...)
```

---

## 🔧 提交 Pull Request

### 1. Fork 并克隆

```bash
# Fork 本仓库（在 GitHub 网页上点击 Fork）

# 克隆你的 Fork
git clone https://github.com/YOUR_USERNAME/csharp-concurrency-cookbook.git
cd csharp-concurrency-cookbook
```

### 2. 创建特性分支

```bash
# 从 dev 分支创建
git checkout dev
git pull origin dev

# 创建新分支
git checkout -b feature/chapter-X-name
```

### 3. 添加代码并测试

```bash
# 确保代码能够运行
dotnet build
dotnet run --project ChapterName

# 检查代码风格
dotnet format
```

### 4. 更新 README（如果需要）

参考上面的"README 更新流程"部分。

### 5. 提交变更

```bash
git add .
git commit -m "feat: add chapter X - 章节名称

- 添加 Demo1.cs: 示例 1 说明
- 添加 Demo2.cs: 示例 2 说明
- 更新 README.md"
```

### 6. 推送并创建 PR

```bash
git push origin feature/chapter-X-name
```

然后在 GitHub 上创建 Pull Request，目标分支为 `dev`。

---

## ✅ PR 审查清单

提交 PR 前，请确认：

- [ ] 代码能够编译运行
- [ ] 添加了必要的注释
- [ ] 更新了主 README.md（如果添加新章节）
- [ ] 遵循了代码规范
- [ ] 提交信息清晰
- [ ] 所有文件使用 UTF-8 编码

---

## 🎨 示例：添加第二章的完整流程

### 1. 创建文件夹和代码

```bash
mkdir Threads
cd Threads

# 创建示例文件
# - Program.cs
# - ThreadDemo.cs
# - ThreadPoolDemo.cs
```

### 2. 更新主 README.md

**位置 1：项目结构**
```diff
├── Overview/                   # 第一章：并发编程全景图
+├── Threads/                    # 第二章：线程的底层原理
├── (更多章节代码将陆续添加...)
```

**位置 2：当前可用内容**
```markdown
### ✅ 第二章：线程的底层原理

**学习目标**：理解 Thread、ThreadPool、Task 的区别和使用场景

**代码位置**：`Threads/` 文件夹

| 文件 | 说明 | 核心内容 |
|------|------|---------|
| `ThreadDemo.cs` | Thread 基础 | 创建、启动、管理线程 |
| `ThreadPoolDemo.cs` | ThreadPool 原理 | 工作窃取算法 |

**运行方式**：
\`\`\`bash
dotnet run --project Threads
\`\`\`
```

**位置 3：路线图**
```diff
-| 🚧 第二章 | 进行中 | 线程的底层原理 | `Threads/` |
+| ✅ 第二章 | **已完成** | 线程的底层原理 | `Threads/` |
-### 📊 整体进度：1/21 章节（4.8%）
+### 📊 整体进度：2/21 章节（9.5%）
```

### 3. 提交 PR

```bash
git checkout -b feature/chapter-2-threads
git add .
git commit -m "feat: add chapter 2 - 线程的底层原理

- 添加 ThreadDemo.cs: Thread 基础示例
- 添加 ThreadPoolDemo.cs: ThreadPool 原理
- 更新 README.md"
git push origin feature/chapter-2-threads
```

---

## 📞 需要帮助？

- **GitHub Issues**: [提出问题](https://github.com/Naughtyhusky/csharp-concurrency-cookbook/issues)
- **Discussions**: [参与讨论](https://github.com/Naughtyhusky/csharp-concurrency-cookbook/discussions)

---

**感谢您的贡献！** ❤️