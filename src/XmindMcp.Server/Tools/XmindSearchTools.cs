using System.ComponentModel;
using System.Text.Json;
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
    [Description("按标题关键词搜索主题")]
    public static string SearchTopicsByTitle(
        [Description("XMind 文件的完整路径")]
        string filePath,
        [Description("搜索关键词")]
        string keyword,
        [Description("是否区分大小写（默认 false）")]
        bool caseSensitive = false)
    {
        try
        {
            var doc = XmindReader.Load(filePath);
            var sheet = doc.GetActiveSheet();
            if (sheet == null)
            {
                return JsonSerializer.Serialize(new { error = "No sheets found" });
            }
            var results = TopicSearchEngine.FindByTitle(sheet, keyword, caseSensitive);
            var output = results.Select(t => new
            {
                id = t.Id,
                title = t.Title,
                path = t.Path,
                depth = GetDepth(t),
                hasNotes = t.Notes?.Plain?.Content != null
            }).ToList();
            return JsonSerializer.Serialize(new
            {
                keyword,
                caseSensitive,
                resultCount = output.Count,
                results = output
            }, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    [McpServerTool]
    [Description("使用正则表达式搜索主题标题")]
    public static string SearchTopicsByRegex(
        [Description("XMind 文件的完整路径")]
        string filePath,
        [Description("正则表达式模式")]
        string pattern)
    {
        try
        {
            var doc = XmindReader.Load(filePath);
            var sheet = doc.GetActiveSheet();
            if (sheet == null)
            {
                return JsonSerializer.Serialize(new { error = "No sheets found" });
            }
            var results = TopicSearchEngine.FindByTitleRegex(sheet, pattern);
            var output = results.Select(t => new
            {
                id = t.Id,
                title = t.Title,
                path = t.Path,
                depth = GetDepth(t)
            }).ToList();
            return JsonSerializer.Serialize(new
            {
                pattern,
                resultCount = output.Count,
                results = output
            }, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    [McpServerTool]
    [Description("按标记搜索主题")]
    public static string SearchTopicsByMarker(
        [Description("XMind 文件的完整路径")]
        string filePath,
        [Description("标记 ID（例如：priority-1, task-done, flag-red）")]
        string markerId)
    {
        try
        {
            var doc = XmindReader.Load(filePath);
            var sheet = doc.GetActiveSheet();
            if (sheet == null)
            {
                return JsonSerializer.Serialize(new { error = "No sheets found" });
            }
            var results = TopicSearchEngine.FindByMarker(sheet, markerId);
            var output = results.Select(t => new
            {
                id = t.Id,
                title = t.Title,
                path = t.Path,
                markers = t.Markers?.Select(m => $"{m.GroupId}:{m.MarkerId}").ToList()
            }).ToList();
            return JsonSerializer.Serialize(new
            {
                markerId,
                resultCount = output.Count,
                results = output
            }, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    [McpServerTool]
    [Description("按标签搜索主题")]
    public static string SearchTopicsByLabel(
        [Description("XMind 文件的完整路径")]
        string filePath,
        [Description("标签内容")]
        string label)
    {
        try
        {
            var doc = XmindReader.Load(filePath);
            var sheet = doc.GetActiveSheet();
            if (sheet == null)
            {
                return JsonSerializer.Serialize(new { error = "No sheets found" });
            }
            var results = TopicSearchEngine.FindByLabel(sheet, label);
            var output = results.Select(t => new
            {
                id = t.Id,
                title = t.Title,
                path = t.Path,
                labels = t.Labels
            }).ToList();
            return JsonSerializer.Serialize(new
            {
                label,
                resultCount = output.Count,
                results = output
            }, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    [McpServerTool]
    [Description("按备注内容搜索主题")]
    public static string SearchTopicsByNote(
        [Description("XMind 文件的完整路径")]
        string filePath,
        [Description("搜索关键词")]
        string keyword)
    {
        try
        {
            var doc = XmindReader.Load(filePath);
            var sheet = doc.GetActiveSheet();
            if (sheet == null)
            {
                return JsonSerializer.Serialize(new { error = "No sheets found" });
            }
            var results = TopicSearchEngine.FindByNote(sheet, keyword);
            var output = results.Select(t => new
            {
                id = t.Id,
                title = t.Title,
                path = t.Path,
                notePreview = t.Notes?.Plain?.Content.Length > 100
                                  ? t.Notes.Plain.Content[..100] + "..."
                                  : t.Notes?.Plain?.Content
            }).ToList();
            return JsonSerializer.Serialize(new
            {
                keyword,
                resultCount = output.Count,
                results = output
            }, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    [McpServerTool]
    [Description("获取指定主题的详细信息")]
    public static string GetTopicDetails(
        [Description("XMind 文件的完整路径")]
        string filePath,
        [Description("主题 ID")]
        string topicId)
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
            var topic = allTopics.FirstOrDefault(t => t.Id == topicId);
            if (topic == null)
            {
                return JsonSerializer.Serialize(new { error = "Topic not found" });
            }
            var details = new
            {
                id = topic.Id,
                title = topic.Title,
                path = topic.Path,
                depth = GetDepth(topic),
                notes = topic.Notes?.Plain?.Content,
                markers = topic.Markers?.Select(m => new { m.GroupId, m.MarkerId }).ToList(),
                labels = topic.Labels,
                link = topic.Href,
                childCount = topic.Children?.Attached?.Count ?? 0,
                children = topic.Children?.Attached?.Select(c => new
                {
                    id = c.Id,
                    title = c.Title
                }).ToList(),
                parent = topic.Parent != null
                             ? new
                             {
                                 id = topic.Parent.Id,
                                 title = topic.Parent.Title
                             }
                             : null
            };
            return JsonSerializer.Serialize(details, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    private static int GetDepth(Topic topic)
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