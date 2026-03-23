using System.ComponentModel;
using System.Text.RegularExpressions;
using ModelContextProtocol.Server;
using XmindMcp.Server.Models;
using XmindMcp.Server.Services;

namespace XmindMcp.Server.Tools;

/// <summary>
/// XMind 搜索工具
/// </summary>
[McpServerToolType]
public sealed class XmindSearchTools
{
    [McpServerTool]
    [Description("按标题关键词搜索主题（支持跨所有工作表搜索）")]
    public static async Task<string> SearchTopicsByTitle(
        [Description("XMind 文件的完整路径")]
        string filePath,
        [Description("搜索关键词")]
        string keyword,
        [Description("工作表标题（可选，默认搜索所有工作表）")]
        string? sheetTitle = null,
        [Description("是否区分大小写（默认 false）")]
        bool caseSensitive = false,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var doc = await XmindReader.LoadAsync(filePath, cancellationToken);
            var sheets = GetSheets(doc, sheetTitle);
            var output = sheets.SelectMany(sheet =>
                TopicSearchEngine.FindByTitle(sheet, keyword, caseSensitive)
                                 .Select(t => new
                                 {
                                     sheetTitle = sheet.Title,
                                     id = t.Id,
                                     title = t.Title,
                                     path = t.Path,
                                     depth = GetDepthFromRoot(t),
                                     hasNotes = t.Notes?.Plain?.Content != null
                                 })).ToList();
            return ToolJson.Serialize(new
            {
                keyword,
                caseSensitive,
                sheetFilter = sheetTitle,
                resultCount = output.Count,
                results = output
            });
        }
        catch (Exception ex)
        {
            return ToolJson.Error(ex.Message);
        }
    }

    [McpServerTool]
    [Description("使用正则表达式搜索主题标题（支持跨所有工作表搜索）")]
    public static async Task<string> SearchTopicsByRegex(
        [Description("XMind 文件的完整路径")]
        string filePath,
        [Description("正则表达式模式")]
        string pattern,
        [Description("工作表标题（可选，默认搜索所有工作表）")]
        string? sheetTitle = null,
        [Description("是否忽略大小写（默认 true）")]
        bool ignoreCase = true,
        CancellationToken cancellationToken = default)
    {
        Regex regex;
        try
        {
            var options = ignoreCase ? RegexOptions.IgnoreCase : RegexOptions.None;
            regex = new(pattern, options, TimeSpan.FromSeconds(5));
        }
        catch (ArgumentException ex)
        {
            return ToolJson.Error($"Invalid regular expression: {ex.Message}");
        }
        try
        {
            var doc = await XmindReader.LoadAsync(filePath, cancellationToken);
            var sheets = GetSheets(doc, sheetTitle);
            var output = sheets.SelectMany(sheet =>
                TopicSearchEngine.FindByTitleRegex(sheet, regex)
                                 .Select(t => new
                                 {
                                     sheetTitle = sheet.Title,
                                     id = t.Id,
                                     title = t.Title,
                                     path = t.Path,
                                     depth = GetDepthFromRoot(t)
                                 })).ToList();
            return ToolJson.Serialize(new
            {
                pattern,
                ignoreCase,
                sheetFilter = sheetTitle,
                resultCount = output.Count,
                results = output
            });
        }
        catch (Exception ex)
        {
            return ToolJson.Error(ex.Message);
        }
    }

    [McpServerTool]
    [Description("按标记搜索主题（支持跨所有工作表搜索）")]
    public static async Task<string> SearchTopicsByMarker(
        [Description("XMind 文件的完整路径")]
        string filePath,
        [Description("标记 ID（例如：priority-1, task-done, flag-red）")]
        string markerId,
        [Description("工作表标题（可选，默认搜索所有工作表）")]
        string? sheetTitle = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var doc = await XmindReader.LoadAsync(filePath, cancellationToken);
            var sheets = GetSheets(doc, sheetTitle);
            var output = sheets.SelectMany(sheet =>
                TopicSearchEngine.FindByMarker(sheet, markerId)
                                 .Select(t => new
                                 {
                                     sheetTitle = sheet.Title,
                                     id = t.Id,
                                     title = t.Title,
                                     path = t.Path,
                                     markers = t.Markers?.Select(m => $"{m.GroupId}:{m.MarkerId}").ToList()
                                 })).ToList();
            return ToolJson.Serialize(new
            {
                markerId,
                sheetFilter = sheetTitle,
                resultCount = output.Count,
                results = output
            });
        }
        catch (Exception ex)
        {
            return ToolJson.Error(ex.Message);
        }
    }

    [McpServerTool]
    [Description("按标签搜索主题（支持跨所有工作表搜索）")]
    public static async Task<string> SearchTopicsByLabel(
        [Description("XMind 文件的完整路径")]
        string filePath,
        [Description("标签内容")]
        string label,
        [Description("工作表标题（可选，默认搜索所有工作表）")]
        string? sheetTitle = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var doc = await XmindReader.LoadAsync(filePath, cancellationToken);
            var sheets = GetSheets(doc, sheetTitle);
            var output = sheets.SelectMany(sheet =>
                TopicSearchEngine.FindByLabel(sheet, label)
                                 .Select(t => new
                                 {
                                     sheetTitle = sheet.Title,
                                     id = t.Id,
                                     title = t.Title,
                                     path = t.Path,
                                     labels = t.Labels
                                 })).ToList();
            return ToolJson.Serialize(new
            {
                label,
                sheetFilter = sheetTitle,
                resultCount = output.Count,
                results = output
            });
        }
        catch (Exception ex)
        {
            return ToolJson.Error(ex.Message);
        }
    }

    [McpServerTool]
    [Description("按备注内容搜索主题（支持跨所有工作表搜索）")]
    public static async Task<string> SearchTopicsByNote(
        [Description("XMind 文件的完整路径")]
        string filePath,
        [Description("搜索关键词")]
        string keyword,
        [Description("工作表标题（可选，默认搜索所有工作表）")]
        string? sheetTitle = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var doc = await XmindReader.LoadAsync(filePath, cancellationToken);
            var sheets = GetSheets(doc, sheetTitle);
            var output = sheets.SelectMany(sheet =>
                TopicSearchEngine.FindByNote(sheet, keyword)
                                 .Select(t => new
                                 {
                                     sheetTitle = sheet.Title,
                                     id = t.Id,
                                     title = t.Title,
                                     path = t.Path,
                                     notePreview = t.Notes?.Plain?.Content is { Length: > 100 } c ? c[..100] + "..." : t.Notes?.Plain?.Content
                                 })).ToList();
            return ToolJson.Serialize(new
            {
                keyword,
                sheetFilter = sheetTitle,
                resultCount = output.Count,
                results = output
            });
        }
        catch (Exception ex)
        {
            return ToolJson.Error(ex.Message);
        }
    }

    [McpServerTool]
    [Description("获取指定主题的详细信息（包含父节点、子节点、路径等）")]
    public static async Task<string> GetTopicDetails(
        [Description("XMind 文件的完整路径")]
        string filePath,
        [Description("主题 ID")]
        string topicId,
        [Description("工作表标题（可选，默认在第一个工作表中查找）")]
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
            var topic = TopicSearchEngine.GetAllTopics(sheet).FirstOrDefault(t => t.Id == topicId);
            if (topic == null)
            {
                return ToolJson.Error($"Topic '{topicId}' not found");
            }
            return ToolJson.Serialize(new
            {
                id = topic.Id,
                title = topic.Title,
                path = topic.Path,
                depth = GetDepthFromRoot(topic),
                notes = topic.Notes?.Plain?.Content,
                markers = topic.Markers?.Select(m => new { m.GroupId, m.MarkerId }).ToList(),
                labels = topic.Labels,
                link = topic.Href,
                childCount = topic.Children?.Attached?.Count ?? 0,
                children = topic.Children?.Attached?.Select(c => new { id = c.Id, title = c.Title }).ToList(),
                parent = topic.Parent != null ? new { id = topic.Parent.Id, title = topic.Parent.Title } : null
            });
        }
        catch (Exception ex)
        {
            return ToolJson.Error(ex.Message);
        }
    }

    [McpServerTool]
    [Description("列出指定工作表中的所有关系")]
    public static async Task<string> ListRelationships(
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
            return ToolJson.Serialize(new
            {
                sheetTitle = sheet.Title,
                relationshipCount = sheet.Relationships?.Count ?? 0,
                relationships = sheet.Relationships?.Select(r => new
                                {
                                    id = r.Id,
                                    end1Id = r.End1Id,
                                    end2Id = r.End2Id,
                                    title = r.Title
                                }).ToList() ??
                                []
            });
        }
        catch (Exception ex)
        {
            return ToolJson.Error(ex.Message);
        }
    }

    private static List<Sheet> GetSheets(XmindDocument doc, string? sheetTitle) => sheetTitle != null ? doc.FindSheet(sheetTitle) is { } sheet ? [sheet] : [] : doc.Sheets;

    private static int GetDepthFromRoot(Topic topic)
    {
        var depth = 0;
        var current = topic.Parent;
        while (current != null)
        {
            depth++;
            current = current.Parent;
        }
        return depth;
    }
}