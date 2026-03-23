using System.ComponentModel;
using System.Text;
using ModelContextProtocol.Server;
using XmindMcp.Server.Models;
using XmindMcp.Server.Services;

namespace XmindMcp.Server.Tools;

/// <summary>
/// XMind 读取工具
/// </summary>
[McpServerToolType]
public sealed class XmindReadTools
{
    [McpServerTool]
    [Description("读取 XMind 文件并返回其结构概览")]
    public static async Task<string> ReadXmindFile(
        [Description("XMind 文件的完整路径")]
        string filePath,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var doc = await XmindReader.LoadAsync(filePath, cancellationToken);
            var sheet = doc.GetActiveSheet();
            if (sheet == null)
            {
                return ToolJson.Error("No sheets found in the XMind file");
            }
            var result = new
            {
                filePath = doc.FilePath,
                sheetCount = doc.Sheets.Count,
                activeSheet = new
                {
                    id = sheet.Id,
                    title = sheet.Title,
                    topicCount = TopicSearchEngine.CountTopics(sheet),
                    maxDepth = TopicSearchEngine.GetDepth(sheet.RootTopic),
                    rootTopic = SerializeTopicSummary(sheet.RootTopic)
                }
            };
            return ToolJson.Serialize(result);
        }
        catch (Exception ex)
        {
            return ToolJson.Error(ex.Message);
        }
    }

    [McpServerTool]
    [Description("获取 XMind 文件指定工作表的完整主题树")]
    public static async Task<string> GetTopicTree(
        [Description("XMind 文件的完整路径")]
        string filePath,
        [Description("工作表标题（可选，默认使用第一个工作表）")]
        string? sheetTitle = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var doc = await XmindReader.LoadAsync(filePath, cancellationToken);
            var sheet = sheetTitle != null ? doc.FindSheet(sheetTitle) : doc.GetActiveSheet();
            if (sheet == null)
            {
                return ToolJson.Error($"Sheet '{sheetTitle}' not found");
            }
            var tree = SerializeTopicTree(sheet.RootTopic);
            return ToolJson.Serialize(tree);
        }
        catch (Exception ex)
        {
            return ToolJson.Error(ex.Message);
        }
    }

    [McpServerTool]
    [Description("获取 XMind 文件所有工作表的统计信息（主题数量、深度、标注等）")]
    public static async Task<string> GetXmindStatistics(
        [Description("XMind 文件的完整路径")]
        string filePath,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var doc = await XmindReader.LoadAsync(filePath, cancellationToken);
            var stats = new
            {
                filePath = doc.FilePath,
                sheetCount = doc.Sheets.Count,
                sheets = doc.Sheets.Select(s =>
                {
                    var allTopics = TopicSearchEngine.GetAllTopics(s);
                    var leafNodes = TopicSearchEngine.GetLeafNodes(s.RootTopic);
                    return new
                    {
                        title = s.Title,
                        totalTopics = allTopics.Count,
                        leafNodes = leafNodes.Count,
                        maxDepth = TopicSearchEngine.GetDepth(s.RootTopic),
                        topicsWithNotes = allTopics.Count(t => t.Notes?.Plain?.Content != null),
                        topicsWithMarkers = allTopics.Count(t => t.Markers?.Count > 0),
                        topicsWithLabels = allTopics.Count(t => t.Labels?.Count > 0),
                        relationships = s.Relationships?.Count ?? 0,
                        rootTitle = s.RootTopic.Title
                    };
                }).ToList()
            };
            return ToolJson.Serialize(stats);
        }
        catch (Exception ex)
        {
            return ToolJson.Error(ex.Message);
        }
    }

    [McpServerTool]
    [Description("列出 XMind 文件中的所有工作表")]
    public static async Task<string> ListSheets(
        [Description("XMind 文件的完整路径")]
        string filePath,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var doc = await XmindReader.LoadAsync(filePath, cancellationToken);
            var sheets = doc.Sheets.Select(s => new
            {
                id = s.Id,
                title = s.Title,
                rootTopicTitle = s.RootTopic.Title,
                topicCount = TopicSearchEngine.CountTopics(s)
            }).ToList();
            return ToolJson.Serialize(sheets);
        }
        catch (Exception ex)
        {
            return ToolJson.Error(ex.Message);
        }
    }

    [McpServerTool]
    [Description("将 XMind 工作表导出为 Markdown 大纲格式")]
    public static async Task<string> ExportSheetToMarkdown(
        [Description("XMind 文件的完整路径")]
        string filePath,
        [Description("工作表标题（可选，默认使用第一个工作表）")]
        string? sheetTitle = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var doc = await XmindReader.LoadAsync(filePath, cancellationToken);
            var sheet = sheetTitle != null ? doc.FindSheet(sheetTitle) : doc.GetActiveSheet();
            if (sheet == null)
            {
                return ToolJson.Error($"Sheet '{sheetTitle}' not found");
            }
            var sb = new StringBuilder();
            sb.AppendLine($"# {sheet.RootTopic.Title}");
            AppendTopicMarkdown(sb, sheet.RootTopic.Children?.Attached, 2);
            return sb.ToString();
        }
        catch (Exception ex)
        {
            return ToolJson.Error(ex.Message);
        }
    }

    private static void AppendTopicMarkdown(StringBuilder sb, List<Topic>? topics, int level)
    {
        if (topics == null)
        {
            return;
        }
        var prefix = new string('#', Math.Min(level, 6));
        foreach (var topic in topics)
        {
            sb.AppendLine($"{prefix} {topic.Title}");
            if (topic.Notes?.Plain?.Content is { Length: > 0 } notes)
            {
                sb.AppendLine();
                sb.AppendLine(notes);
                sb.AppendLine();
            }
            AppendTopicMarkdown(sb, topic.Children?.Attached, level + 1);
        }
    }

    internal static object SerializeTopicSummary(Topic topic) =>
        new
        {
            id = topic.Id,
            title = topic.Title,
            childCount = topic.Children?.Attached?.Count ?? 0,
            hasNotes = topic.Notes?.Plain?.Content != null,
            hasMarkers = topic.Markers?.Count > 0,
            hasLabels = topic.Labels?.Count > 0,
            hasLink = topic.Href != null
        };

    internal static object SerializeTopicTree(Topic topic)
    {
        var result = new Dictionary<string, object>
        {
            ["id"] = topic.Id,
            ["title"] = topic.Title
        };
        if (topic.Notes?.Plain?.Content != null)
        {
            result["notes"] = topic.Notes.Plain.Content;
        }
        if (topic.Markers?.Count > 0)
        {
            result["markers"] = topic.Markers.Select(m => $"{m.GroupId}:{m.MarkerId}").ToList();
        }
        if (topic.Labels?.Count > 0)
        {
            result["labels"] = topic.Labels;
        }
        if (topic.Href != null)
        {
            result["link"] = topic.Href;
        }
        if (topic.Children?.Attached?.Count > 0)
        {
            result["children"] = topic.Children.Attached.Select(SerializeTopicTree).ToList();
        }
        return result;
    }
}