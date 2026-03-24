using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using XmindMcp.Server.Tools;

var builder = Host.CreateApplicationBuilder(args);

// 添加 MCP 服务器
builder.Services.AddMcpServer()
       .WithStdioServerTransport()
       .WithTools<XmindReadTools>()
       .WithTools<XmindWriteTools>()
       .WithTools<XmindSearchTools>();

// 清除默认日志提供者（Host.CreateApplicationBuilder 默认注册了写入 stdout 的 Console 提供者，
// 会污染 MCP stdio 协议流），然后重新添加仅输出到标准错误的 Console 提供者
builder.Logging.ClearProviders();
builder.Logging.AddConsole(options => options.LogToStandardErrorThreshold = LogLevel.Trace);

// 构建并运行
await builder.Build().RunAsync();