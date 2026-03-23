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

// 配置日志输出到标准错误
builder.Logging.AddConsole(options => { options.LogToStandardErrorThreshold = LogLevel.Trace; });

// 构建并运行
await builder.Build().RunAsync();