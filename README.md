# XMind MCP Server

一个基于 .NET 的 Model Context Protocol (MCP) 服务器，用于读取、写入和搜索 XMind 思维导图文件。

## 功能特性

### 读取工具
- `ReadXmindFile` - 读取 XMind 文件并返回其结构概览
- `GetTopicTree` - 获取完整的主题树结构
- `GetStatistics` - 获取文件统计信息
- `ListSheets` - 列出所有工作表

### 搜索工具
- `SearchTopicsByTitle` - 按标题关键词搜索
- `SearchTopicsByRegex` - 使用正则表达式搜索
- `SearchTopicsByMarker` - 按标记搜索
- `SearchTopicsByLabel` - 按标签搜索
- `SearchTopicsByNote` - 按备注内容搜索
- `GetTopicDetails` - 获取主题详细信息

### 写入工具
- `CreateXmindFile` - 创建新的 XMind 文件
- `AddChildTopic` - 添加子主题
- `UpdateTopicTitle` - 更新主题标题
- `UpdateTopicNotes` - 更新主题备注
- `AddMarkerToTopic` - 添加标记
- `AddLabelToTopic` - 添加标签
- `DeleteTopic` - 删除主题
- `AddMultipleChildTopics` - 批量添加子主题

## 系统要求

- .NET 11.0 或更高版本

## 安装

### 1. 克隆仓库
```bash
git clone <repository-url>
cd xmind-mcp
```

### 2. 构建项目
```bash
dotnet build
```

### 3. 发布
```bash
dotnet publish src/XmindMcp.Server -c Release -o publish
```

## 在 OpenCode 中配置

在 `opencode.json` 中添加以下配置：

```json
{
  "mcp": {
    "xmind": {
      "type": "local",
      "command": "dotnet",
      "args": ["run", "--project", "yourpath\\xmind-mcp\\src\\XmindMcp.Server"],
      "enabled": true
    }
  }
}
```

建议将代码发布为单文件后,使用如下配置(注意修改文件路径):

```json
  "mcp": {
    "microsoft-learn": {
      "type": "remote",
      "url": "https://learn.microsoft.com/api/mcp",
      "enabled": true
    },
    "xmind": {
      "command": ["D:\\Tools\\XmindMcp.Server.exe"],
      "enabled": true,
      "type": "local"
    }
  },
```

或者使用发布后的可执行文件：

```json
{
  "mcp": {
    "xmind": {
      "type": "local",
      "command": "yourpath\\xmind-mcp\\publish\\XmindMcp.Server.exe",
      "enabled": true
    }
  }
}
```

## 使用示例

### 创建新的 XMind 文件
```json
{
  "tool": "CreateXmindFile",
  "parameters": {
    "filePath": "yourpath\\my-mindmap.xmind",
    "rootTitle": "项目计划",
    "sheetTitle": "主工作表"
  }
}
```

### 添加子主题
```json
{
  "tool": "AddChildTopic",
  "parameters": {
    "filePath": "yourpath\\my-mindmap.xmind",
    "parentTopicId": "topic-id",
    "title": "需求分析",
    "notes": "分析项目需求"
  }
}
```

### 搜索主题
```json
{
  "tool": "SearchTopicsByTitle",
  "parameters": {
    "filePath": "yourpath\\my-mindmap.xmind",
    "keyword": "C#"
  }
}
```

### 添加标记
```json
{
  "tool": "AddMarkerToTopic",
  "parameters": {
    "filePath": "yourpath\\my-mindmap.xmind",
    "topicId": "topic-id",
    "groupId": "priorityMarkers",
    "markerId": "priority-1"
  }
}
```

## 项目结构

```
xmind-mcp/
├── src/
│   └── XmindMcp.Server/
│       ├── Models/          # 数据模型
│       │   ├── Topic.cs
│       │   ├── Sheet.cs
│       │   ├── Marker.cs
│       │   ├── Relationship.cs
│       │   └── XmindDocument.cs
│       ├── Services/        # 业务逻辑
│       │   ├── XmindReader.cs
│       │   ├── XmindWriter.cs
│       │   ├── TopicSearchEngine.cs
│       │   └── TopicEditor.cs
│       ├── Tools/           # MCP 工具
│       │   ├── XmindReadTools.cs
│       │   ├── XmindSearchTools.cs
│       │   └── XmindWriteTools.cs
│       └── Program.cs       # 入口点
├── tests/
│   └── XmindMcp.Tests/      # 单元测试
└── XmindMcp.sln             # 解决方案文件
```

## 支持的标记类型

### 优先级标记
- `priority-1` - 优先级 1
- `priority-2` - 优先级 2
- `priority-3` - 优先级 3

### 任务状态标记
- `task-done` - 已完成
- `task-oct` - 待办
- `task-Doing` - 进行中

### 旗帜标记
- `flag-red` - 红旗
- `flag-orange` - 橙旗
- `flag-green` - 绿旗
- `flag-blue` - 蓝旗

### 表情标记
- `smiley-smile` - 微笑
- `smiley-sad` - 悲伤
- `smiley-surprise` - 惊讶

## 技术栈

- **.NET 11.0** - 运行时
- **ModelContextProtocol** - MCP SDK
- **System.Text.Json** - JSON 序列化
- **System.IO.Compression** - ZIP 文件处理
- **xUnit** - 单元测试

## 依赖包

- `ModelContextProtocol` (1.1.0)
- `Microsoft.Extensions.Hosting` (10.0.5)

## 许可证

MIT License
