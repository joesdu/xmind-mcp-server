using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;
using XmindMcp.Server.Models;
using XmindMcp.Server.Services;

namespace XmindMcp.Server.Tools;

/// <summary>
/// XMind 写入工具
/// </summary>
[McpServerToolType]
public sealed class XmindWriteTools
{
    [McpServerTool]
    [Description("创建新的 XMind 文件")]
    public static string CreateXmindFile(
        [Description("输出文件路径")]
        string filePath,
        [Description("根主题标题")]
        string rootTitle,
        [Description("工作表标题（默认：Sheet 1）")]
        string sheetTitle = "Sheet 1")
    {
        try
        {
            var doc = new XmindDocument();
            var sheet = doc.AddSheet(sheetTitle, rootTitle);
            XmindWriter.Save(doc, filePath);
            return JsonSerializer.Serialize(new
            {
                success = true,
                filePath,
                sheetId = sheet.Id,
                rootTopicId = sheet.RootTopic.Id,
                message = $"XMind file created: {filePath}"
            }, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    [McpServerTool]
    [Description("向指定主题添加子主题")]
    public static string AddChildTopic(
        [Description("XMind 文件的完整路径")]
        string filePath,
        [Description("父主题 ID")]
        string parentTopicId,
        [Description("新主题标题")]
        string title,
        [Description("备注内容（可选）")]
        string? notes = null)
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
            var parent = allTopics.FirstOrDefault(t => t.Id == parentTopicId);
            if (parent == null)
            {
                return JsonSerializer.Serialize(new { error = "Parent topic not found" });
            }
            var child = TopicEditor.AddChild(parent, title);
            if (!string.IsNullOrEmpty(notes))
            {
                TopicEditor.UpdateNotes(child, notes);
            }
            XmindWriter.Save(doc);
            return JsonSerializer.Serialize(new
            {
                success = true,
                newTopic = new
                {
                    id = child.Id,
                    title = child.Title,
                    parentTitle = parent.Title,
                    path = child.Path
                }
            }, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    [McpServerTool]
    [Description("更新主题标题")]
    public static string UpdateTopicTitle(
        [Description("XMind 文件的完整路径")]
        string filePath,
        [Description("主题 ID")]
        string topicId,
        [Description("新标题")]
        string newTitle)
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
            var oldTitle = topic.Title;
            TopicEditor.UpdateTitle(topic, newTitle);
            XmindWriter.Save(doc);
            return JsonSerializer.Serialize(new
            {
                success = true,
                topicId,
                oldTitle,
                newTitle,
                message = "Topic title updated"
            }, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    [McpServerTool]
    [Description("更新主题备注")]
    public static string UpdateTopicNotes(
        [Description("XMind 文件的完整路径")]
        string filePath,
        [Description("主题 ID")]
        string topicId,
        [Description("备注内容")]
        string notes)
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
            TopicEditor.UpdateNotes(topic, notes);
            XmindWriter.Save(doc);
            return JsonSerializer.Serialize(new
            {
                success = true,
                topicId,
                topicTitle = topic.Title,
                notesUpdated = true,
                message = "Topic notes updated"
            }, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    [McpServerTool]
    [Description("为主题添加标记")]
    public static string AddMarkerToTopic(
        [Description("XMind 文件的完整路径")]
        string filePath,
        [Description("主题 ID")]
        string topicId,
        [Description("标记组 ID（例如：priorityMarkers, taskMarkers, flagMarkers）")]
        string groupId,
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
            var allTopics = TopicSearchEngine.GetAllTopics(sheet);
            var topic = allTopics.FirstOrDefault(t => t.Id == topicId);
            if (topic == null)
            {
                return JsonSerializer.Serialize(new { error = "Topic not found" });
            }
            var marker = new Marker { GroupId = groupId, MarkerId = markerId };
            TopicEditor.AddMarker(topic, marker);
            XmindWriter.Save(doc);
            return JsonSerializer.Serialize(new
            {
                success = true,
                topicId,
                topicTitle = topic.Title,
                marker = new { groupId, markerId },
                message = "Marker added"
            }, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    [McpServerTool]
    [Description("为主题添加标签")]
    public static string AddLabelToTopic(
        [Description("XMind 文件的完整路径")]
        string filePath,
        [Description("主题 ID")]
        string topicId,
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
            var allTopics = TopicSearchEngine.GetAllTopics(sheet);
            var topic = allTopics.FirstOrDefault(t => t.Id == topicId);
            if (topic == null)
            {
                return JsonSerializer.Serialize(new { error = "Topic not found" });
            }
            TopicEditor.AddLabel(topic, label);
            XmindWriter.Save(doc);
            return JsonSerializer.Serialize(new
            {
                success = true,
                topicId,
                topicTitle = topic.Title,
                label,
                message = "Label added"
            }, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    [McpServerTool]
    [Description("删除主题")]
    public static string DeleteTopic(
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
            if (topic == sheet.RootTopic)
            {
                return JsonSerializer.Serialize(new { error = "Cannot delete root topic" });
            }
            var deletedTitle = topic.Title;
            TopicEditor.RemoveTopic(topic);
            XmindWriter.Save(doc);
            return JsonSerializer.Serialize(new
            {
                success = true,
                deletedTopicId = topicId,
                deletedTitle,
                message = "Topic deleted"
            }, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    [McpServerTool]
    [Description("批量添加子主题")]
    public static string AddMultipleChildTopics(
        [Description("XMind 文件的完整路径")]
        string filePath,
        [Description("父主题 ID")]
        string parentTopicId,
        [Description("子主题标题列表（用逗号分隔）")]
        string titles)
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
            var parent = allTopics.FirstOrDefault(t => t.Id == parentTopicId);
            if (parent == null)
            {
                return JsonSerializer.Serialize(new { error = "Parent topic not found" });
            }
            var titleList = titles.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var newTopics = TopicEditor.AddChildren(parent, titleList);
            XmindWriter.Save(doc);
            return JsonSerializer.Serialize(new
            {
                success = true,
                parentTitle = parent.Title,
                addedCount = newTopics.Count,
                newTopics = newTopics.Select(t => new
                {
                    id = t.Id,
                    title = t.Title
                }).ToList()
            }, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }
}