using System.ComponentModel;
using System.Text.Json;
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
    [Description("读取 XMind 文件并返回其结构")]
    public static string ReadXmindFile(
        [Description("XMind 文件的完整路径")]
        string filePath)
    {
        try
        {
            var doc = XmindReader.Load(filePath);
            var sheet = doc.GetActiveSheet();
            if (sheet == null)
            {
                return JsonSerializer.Serialize(new { error = "No sheets found in the XMind file" });
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
            return JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    [McpServerTool]
    [Description("获取 XMind 文件的完整主题树")]
    public static string GetTopicTree(
        [Description("XMind 文件的完整路径")]
        string filePath,
        [Description("工作表标题（可选，默认使用第一个工作表）")]
        string? sheetTitle = null)
    {
        try
        {
            var doc = XmindReader.Load(filePath);
            var sheet = sheetTitle != null
                            ? doc.FindSheet(sheetTitle)
                            : doc.GetActiveSheet();
            if (sheet == null)
            {
                return JsonSerializer.Serialize(new { error = "Sheet not found" });
            }
            var tree = SerializeTopicTree(sheet.RootTopic);
            return JsonSerializer.Serialize(tree, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    [McpServerTool]
    [Description("获取 XMind 文件的统计信息")]
    public static string GetXmindStatistics(
        [Description("XMind 文件的完整路径")]
        string filePath)
    {
        try
        {
            var doc = XmindReader.Load(filePath);
            var sheet = doc.GetActiveSheet();
            if (sheet == null)
            {
                return JsonSerializer.Serialize(new { error = "No sheets found" });
            }
            var allTopics = TopicSearchEngine.GetAllTopics(sheet);
            var leafNodes = TopicSearchEngine.GetLeafNodes(sheet.RootTopic);
            var stats = new
            {
                filePath = doc.FilePath,
                sheetCount = doc.Sheets.Count,
                sheets = doc.Sheets.Select(s => new
                {
                    title = s.Title,
                    topicCount = TopicSearchEngine.CountTopics(s),
                    maxDepth = TopicSearchEngine.GetDepth(s.RootTopic),
                    rootTitle = s.RootTopic.Title
                }).ToList(),
                activeSheet = new
                {
                    title = sheet.Title,
                    totalTopics = allTopics.Count,
                    leafNodes = leafNodes.Count,
                    maxDepth = TopicSearchEngine.GetDepth(sheet.RootTopic),
                    topicsWithNotes = allTopics.Count(t => t.Notes?.Plain?.Content != null),
                    topicsWithMarkers = allTopics.Count(t => t.Markers?.Count > 0),
                    topicsWithLabels = allTopics.Count(t => t.Labels?.Count > 0),
                    relationships = sheet.Relationships?.Count ?? 0
                }
            };
            return JsonSerializer.Serialize(stats, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    [McpServerTool]
    [Description("列出 XMind 文件中的所有工作表")]
    public static string ListSheets(
        [Description("XMind 文件的完整路径")]
        string filePath)
    {
        try
        {
            var doc = XmindReader.Load(filePath);
            var sheets = doc.Sheets.Select(s => new
            {
                id = s.Id,
                title = s.Title,
                rootTopicTitle = s.RootTopic.Title,
                topicCount = TopicSearchEngine.CountTopics(s)
            }).ToList();
            return JsonSerializer.Serialize(sheets, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    private static object SerializeTopicSummary(Topic topic) =>
        new
        {
            id = topic.Id,
            title = topic.Title,
            childCount = topic.Children?.Attached?.Count ?? 0,
            hasNotes = topic.Notes?.Plain?.Content != null,
            hasMarkers = topic.Markers?.Count > 0,
            hasLabels = topic.Labels?.Count > 0
        };

    private static object SerializeTopicTree(Topic topic)
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