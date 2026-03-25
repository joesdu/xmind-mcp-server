# XMind MCP Server

一个基于 .NET 的 MCP（Model Context Protocol）服务器，用于**读取、搜索、修改和导出 `.xmind` 文件**。

它适合用作 AI Agent / MCP Client 的本地 XMind 操作后端，让模型可以直接对思维导图进行结构化读写，而不是把 XMind 当作黑盒文件处理。

---

## 功能概览

- 读取 XMind 文件结构、主题树、统计信息、工作表列表
- 按标题、正则、标签、标记、备注搜索主题
- 获取主题详细信息与关系列表
- 创建/重命名/删除工作表
- 添加、插入、移动、复制、删除主题
- 更新标题、备注、标签、标记、链接
- 添加/删除主题关系（Relationship）
- 将指定工作表导出为 Markdown 大纲

---

## 工具列表

### 读取工具

- `ReadXmindFile`：读取文件概览
- `GetTopicTree`：获取指定工作表的完整主题树
- `GetXmindStatistics`：获取所有工作表统计信息
- `ListSheets`：列出全部工作表
- `ExportSheetToMarkdown`：将指定工作表导出为 Markdown

### 搜索工具

- `SearchTopicsByTitle`：按标题关键词搜索
- `SearchTopicsByRegex`：按正则表达式搜索标题
- `SearchTopicsByMarker`：按标记搜索
- `SearchTopicsByLabel`：按标签搜索
- `SearchTopicsByNote`：按备注内容搜索
- `GetTopicDetails`：获取主题详细信息
- `ListRelationships`：列出指定工作表中的关系

### 写入工具

#### 文件 / 工作表

- `CreateXmindFile`：创建新的 XMind 文件
- `AddSheet`：新增工作表
- `RenameSheet`：重命名工作表
- `DeleteSheet`：删除工作表

#### 主题结构

- `AddChildTopic`：添加子主题
- `AddMultipleChildTopics`：批量添加子主题
- `InsertChildAtPosition`：按索引插入子主题
- `MoveTopic`：移动主题到新的父节点下
- `CloneTopicTool`：复制主题到新的父节点下
- `DeleteTopic`：删除主题

#### 主题内容

- `UpdateTopicTitle`：更新主题标题
- `UpdateTopicNotes`：更新主题备注
- `SetTopicLink`：设置主题链接
- `ClearTopicLink`：清除主题链接

#### 标签 / 标记

- `AddMarkerToTopic`：添加标记
- `RemoveMarkerFromTopic`：移除标记
- `AddLabelToTopic`：添加标签
- `RemoveLabelFromTopic`：移除标签

#### 关系

- `AddRelationship`：新增关系
- `RemoveRelationship`：删除关系

---

## 参数约定

### `filePath`

所有工具都使用 XMind 文件完整路径，例如：

```json
"filePath": "C:\\workspace\\demo.xmind"
```

### `sheetTitle`

多数搜索/写入工具支持可选 `sheetTitle`：

- 不传：默认使用第一个工作表，或在搜索类工具中搜索全部工作表
- 传入：精确定位到指定工作表（大小写不敏感）

### 返回结果

- 成功时通常返回结构化 JSON
- 失败时统一返回：

```json
{ "error": "错误信息" }
```

---

## 系统要求

- .NET 11 SDK（当前项目目标框架为 `net11.0`）

---

## 安装与运行

### 1. 克隆仓库

```bash
git clone https://github.com/joesdu/xmind-mcp-server.git
cd xmind-mcp
```

### 2. 构建

```bash
dotnet build XmindMcp.slnx
```

### 3. 测试

```bash
dotnet test XmindMcp.slnx
```

### 4. 直接运行 MCP Server

```bash
dotnet run --project src/XmindMcp
```

### 5. 发布

```bash
dotnet publish src/XmindMcp -c Release -o publish
```

---

## 在 MCP Client 中配置

### VS Code

在项目根目录创建 `.vscode/mcp.json`（仓库已包含此文件，打开项目后 VS Code 会自动发现并提示启用）：

**使用 `dotnet run`（开发模式）：**

```json
{
  "servers": {
    "xmind": {
      "type": "stdio",
      "command": "dotnet",
      "args": ["run", "--project", "${workspaceFolder}/src/XmindMcp"]
    }
  }
}
```

**使用发布后的可执行文件（推荐）：**

```json
{
  "servers": {
    "xmind": {
      "type": "stdio",
      "command": "yourpath\\xmind-mcp\\publish\\XmindMcp.exe"
    }
  }
}
```

> **注意**：VS Code 的 MCP stdio 传输要求 stdout 只输出合法 JSON，本项目已将所有日志重定向至 stderr，可直接使用。

---

### OpenCode / 其他 MCP Client

**使用 `dotnet run`：**

```json
{
  "mcp": {
    "xmind": {
      "type": "local",
      "command": "dotnet",
      "args": ["run", "--project", "yourpath\\xmind-mcp\\src\\XmindMcp"],
      "enabled": true
    }
  }
}
```

**使用发布后的可执行文件：**

```json
{
  "mcp": {
    "xmind": {
      "type": "local",
      "command": "yourpath\\xmind-mcp\\publish\\XmindMcp.exe",
      "enabled": true
    }
  }
}
```

---

## 使用示例

### 创建 XMind 文件

```json
{
  "tool": "CreateXmindFile",
  "parameters": {
    "filePath": "C:\\workspace\\project-plan.xmind",
    "rootTitle": "项目计划",
    "sheetTitle": "主工作表"
  }
}
```

### 在指定工作表中添加子主题

```json
{
  "tool": "AddChildTopic",
  "parameters": {
    "filePath": "C:\\workspace\\project-plan.xmind",
    "sheetTitle": "主工作表",
    "parentTopicId": "topic-id",
    "title": "需求分析",
    "notes": "分析项目需求与边界"
  }
}
```

### 按标题搜索（跨工作表）

```json
{
  "tool": "SearchTopicsByTitle",
  "parameters": {
    "filePath": "C:\\workspace\\project-plan.xmind",
    "keyword": "C#"
  }
}
```

### 使用正则搜索

```json
{
  "tool": "SearchTopicsByRegex",
  "parameters": {
    "filePath": "C:\\workspace\\project-plan.xmind",
    "pattern": "^(需求|设计)",
    "ignoreCase": true
  }
}
```

### 设置主题链接

```json
{
  "tool": "SetTopicLink",
  "parameters": {
    "filePath": "C:\\workspace\\project-plan.xmind",
    "sheetTitle": "主工作表",
    "topicId": "topic-id",
    "url": "https://example.com/spec"
  }
}
```

### 添加关系

```json
{
  "tool": "AddRelationship",
  "parameters": {
    "filePath": "C:\\workspace\\project-plan.xmind",
    "sheetTitle": "主工作表",
    "end1TopicId": "topic-a",
    "end2TopicId": "topic-b",
    "title": "依赖"
  }
}
```

### 导出工作表为 Markdown

```json
{
  "tool": "ExportSheetToMarkdown",
  "parameters": {
    "filePath": "C:\\workspace\\project-plan.xmind",
    "sheetTitle": "主工作表"
  }
}
```

---

## 项目结构

```text
xmind-mcp/
├── src/
│   └── XmindMcp/
│       ├── Models/      # XMind 数据模型
│       ├── Services/    # Reader / Writer / Search / Editor
│       ├── Tools/       # MCP 工具定义
│       ├── Program.cs   # MCP Server 入口
│       └── XmindMcp.csproj
├── tests/
│   └── XmindMcp.Tests/
└── XmindMcp.slnx
```

---

## 支持的常用标记

### 优先级

- `priority-1`
- `priority-2`
- `priority-3`

### 任务状态

- `task-done`
- `task-oct`
- `task-Doing`

### 旗帜

- `flag-red`
- `flag-orange`
- `flag-green`
- `flag-blue`

### 表情

- `smiley-smile`
- `smiley-sad`
- `smiley-surprise`

---

## 技术栈

- .NET 11
- ModelContextProtocol
- System.Text.Json
- System.IO.Compression
- MSTest

---

## 当前实现说明

- 仅支持现代 `.xmind` 格式（`content.json`），**不支持**旧版 `content.xml`
- 归档内的 `manifest.json`、`metadata.json`、`metadata/content.json` 由程序自动生成
- 工具实现已统一为异步 `Task<string>`，避免阻塞 MCP 服务器线程

---

## 许可证

MIT License
