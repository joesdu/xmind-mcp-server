using System.ComponentModel;
using ModelContextProtocol.Server;
using XmindMcp.Models;
using XmindMcp.Services;

namespace XmindMcp.Tools;

/// <summary>
/// XMind 写入工具
/// </summary>
[McpServerToolType]
public sealed class XmindWriteTools
{
    [McpServerTool]
    [Description("创建新的 XMind 文件")]
    public static async Task<string> CreateXmindFile(
        [Description("输出文件路径")]
        string filePath,
        [Description("根主题标题")]
        string rootTitle,
        [Description("工作表标题(默认: Sheet 1)")]
        string sheetTitle = "Sheet 1",
        CancellationToken cancellationToken = default)
    {
        try
        {
            var doc = new XmindDocument();
            var sheet = doc.AddSheet(sheetTitle, rootTitle);
            await XmindWriter.SaveAsync(doc, filePath, cancellationToken);
            return ToolJson.Serialize(new
            {
                success = true,
                filePath,
                sheetId = sheet.Id,
                rootTopicId = sheet.RootTopic.Id,
                message = $"XMind file created: {filePath}"
            });
        }
        catch (Exception ex)
        {
            return ToolJson.Error(ex.Message);
        }
    }

    [McpServerTool]
    [Description("""
                 向指定主题添加子主题。

                 ⚠️ 重要: parentTopicId 必须是主题的 ID(GUID),不是标题!

                 正确使用流程: 
                 1. 先用 SearchTopicsByTitle 搜索父主题
                 2. 从返回结果中获取 id 字段值
                 3. 调用此工具传入该 id 和新子主题的标题
                 """)]
    public static async Task<string> AddChildTopic(
        [Description("XMind 文件的完整路径")]
        string filePath,
        [Description("父主题 ID(必须是 SearchTopicsByTitle 返回的 id 字段,不是标题!)")]
        string parentTopicId,
        [Description("新主题标题")]
        string title,
        [Description("工作表标题(可选,默认使用第一个工作表)")]
        string? sheetTitle = null,
        [Description("备注内容(可选)")]
        string? notes = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var doc = await XmindReader.LoadAsync(filePath, cancellationToken);
            var sheet = GetTargetSheet(doc, sheetTitle);
            if (sheet == null)
            {
                return MissingSheet(sheetTitle);
            }
            var parent = FindTopic(sheet, parentTopicId);
            if (parent == null)
            {
                return ToolJson.Error($"Parent topic ID '{parentTopicId}' not found. Use SearchTopicsByTitle first to get the correct topic ID.");
            }
            var child = TopicEditor.AddChild(parent, title);
            if (!string.IsNullOrEmpty(notes))
            {
                TopicEditor.UpdateNotes(child, notes);
            }
            await XmindWriter.SaveAsync(doc, cancellationToken: cancellationToken);
            return ToolJson.Serialize(new
            {
                success = true,
                sheetTitle = sheet.Title,
                newTopic = new
                {
                    id = child.Id,
                    title = child.Title,
                    parentTitle = parent.Title,
                    path = child.Path
                }
            });
        }
        catch (Exception ex)
        {
            return ToolJson.Error(ex.Message);
        }
    }

    [McpServerTool]
    [Description("""
                 批量添加子主题到指定父主题。

                 ⚠️ 重要: parentTopicId 必须是主题的 ID(GUID),不是标题!

                 正确使用流程: 
                 1. 先用 SearchTopicsByTitle 搜索父主题
                 2. 从返回结果中获取 id 字段值
                 3. 调用此工具传入该 id 和用逗号分隔的子主题标题列表
                 """)]
    public static async Task<string> AddMultipleChildTopics(
        [Description("XMind 文件的完整路径")]
        string filePath,
        [Description("父主题 ID(必须是 SearchTopicsByTitle 返回的 id 字段,不是标题!)")]
        string parentTopicId,
        [Description("子主题标题列表(用逗号分隔)")]
        string titles,
        [Description("工作表标题(可选,默认使用第一个工作表)")]
        string? sheetTitle = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var doc = await XmindReader.LoadAsync(filePath, cancellationToken);
            var sheet = GetTargetSheet(doc, sheetTitle);
            if (sheet == null)
            {
                return MissingSheet(sheetTitle);
            }
            var parent = FindTopic(sheet, parentTopicId);
            if (parent == null)
            {
                return ToolJson.Error($"Parent topic ID '{parentTopicId}' not found. Use SearchTopicsByTitle first to get the correct topic ID.");
            }
            var titleList = titles.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var newTopics = TopicEditor.AddChildren(parent, titleList);
            await XmindWriter.SaveAsync(doc, cancellationToken: cancellationToken);
            return ToolJson.Serialize(new
            {
                success = true,
                sheetTitle = sheet.Title,
                parentTitle = parent.Title,
                addedCount = newTopics.Count,
                newTopics = newTopics.Select(t => new { id = t.Id, title = t.Title }).ToList()
            });
        }
        catch (Exception ex)
        {
            return ToolJson.Error(ex.Message);
        }
    }

    [McpServerTool]
    [Description("""
                 在指定位置插入子主题。

                 ⚠️ 重要: parentTopicId 必须是主题的 ID(GUID),不是标题!

                 正确使用流程: 
                 1. 先用 SearchTopicsByTitle 搜索父主题
                 2. 从返回结果中获取 id 字段值
                 3. 调用此工具传入该 id、插入位置和新主题标题
                 """)]
    public static async Task<string> InsertChildAtPosition(
        [Description("XMind 文件的完整路径")]
        string filePath,
        [Description("父主题 ID(必须是 SearchTopicsByTitle 返回的 id 字段,不是标题!)")]
        string parentTopicId,
        [Description("插入位置索引(从 0 开始)")]
        int index,
        [Description("新主题标题")]
        string title,
        [Description("工作表标题(可选,默认使用第一个工作表)")]
        string? sheetTitle = null,
        [Description("备注内容(可选)")]
        string? notes = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var doc = await XmindReader.LoadAsync(filePath, cancellationToken);
            var sheet = GetTargetSheet(doc, sheetTitle);
            if (sheet == null)
            {
                return MissingSheet(sheetTitle);
            }
            var parent = FindTopic(sheet, parentTopicId);
            if (parent == null)
            {
                return ToolJson.Error($"Parent topic ID '{parentTopicId}' not found. Use SearchTopicsByTitle first to get the correct topic ID.");
            }
            var child = TopicEditor.InsertChild(parent, index, title);
            if (!string.IsNullOrEmpty(notes))
            {
                TopicEditor.UpdateNotes(child, notes);
            }
            await XmindWriter.SaveAsync(doc, cancellationToken: cancellationToken);
            return ToolJson.Serialize(new
            {
                success = true,
                sheetTitle = sheet.Title,
                insertedTopic = new
                {
                    id = child.Id,
                    title = child.Title,
                    parentTitle = parent.Title,
                    requestedIndex = index,
                    actualIndex = parent.Children?.Attached?.IndexOf(child) ?? 0
                }
            });
        }
        catch (Exception ex)
        {
            return ToolJson.Error(ex.Message);
        }
    }

    [McpServerTool]
    [Description("""
                 更新指定主题的标题。

                 ⚠️ 重要: topicId 必须是主题的 ID(GUID),不是当前标题!

                 正确使用流程: 
                 1. 先用 SearchTopicsByTitle 搜索主题
                 2. 从返回结果中获取 id 字段值
                 3. 调用此工具传入该 id 和新的标题
                 """)]
    public static async Task<string> UpdateTopicTitle(
        [Description("XMind 文件的完整路径")]
        string filePath,
        [Description("主题 ID(必须是 SearchTopicsByTitle 返回的 id 字段,不是标题!)")]
        string topicId,
        [Description("新标题")]
        string newTitle,
        [Description("工作表标题(可选,默认使用第一个工作表)")]
        string? sheetTitle = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var doc = await XmindReader.LoadAsync(filePath, cancellationToken);
            var sheet = GetTargetSheet(doc, sheetTitle);
            if (sheet == null)
            {
                return MissingSheet(sheetTitle);
            }
            var topic = FindTopic(sheet, topicId);
            if (topic == null)
            {
                return ToolJson.Error($"Topic ID '{topicId}' not found. Use SearchTopicsByTitle first to get the correct topic ID.");
            }
            var oldTitle = topic.Title;
            TopicEditor.UpdateTitle(topic, newTitle);
            await XmindWriter.SaveAsync(doc, cancellationToken: cancellationToken);
            return ToolJson.Serialize(new
            {
                success = true,
                sheetTitle = sheet.Title,
                topicId,
                oldTitle,
                newTitle,
                message = "Topic title updated"
            });
        }
        catch (Exception ex)
        {
            return ToolJson.Error(ex.Message);
        }
    }

    [McpServerTool]
    [Description("""
                 更新指定主题的备注内容。

                 ⚠️ 重要: topicId 必须是主题的 ID(GUID),不是标题!

                 正确使用流程: 
                 1. 先用 SearchTopicsByTitle 搜索主题
                 2. 从返回结果中获取 id 字段值
                 3. 调用此工具传入该 id 和备注内容
                 """)]
    public static async Task<string> UpdateTopicNotes(
        [Description("XMind 文件的完整路径")]
        string filePath,
        [Description("主题 ID(必须是 SearchTopicsByTitle 返回的 id 字段,不是标题!)")]
        string topicId,
        [Description("备注内容")]
        string notes,
        [Description("工作表标题(可选,默认使用第一个工作表)")]
        string? sheetTitle = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var doc = await XmindReader.LoadAsync(filePath, cancellationToken);
            var sheet = GetTargetSheet(doc, sheetTitle);
            if (sheet == null)
            {
                return MissingSheet(sheetTitle);
            }
            var topic = FindTopic(sheet, topicId);
            if (topic == null)
            {
                return ToolJson.Error($"Topic ID '{topicId}' not found. Use SearchTopicsByTitle first to get the correct topic ID.");
            }
            TopicEditor.UpdateNotes(topic, notes);
            await XmindWriter.SaveAsync(doc, cancellationToken: cancellationToken);
            return ToolJson.Serialize(new
            {
                success = true,
                sheetTitle = sheet.Title,
                topicId,
                topicTitle = topic.Title,
                notesUpdated = true,
                message = "Topic notes updated"
            });
        }
        catch (Exception ex)
        {
            return ToolJson.Error(ex.Message);
        }
    }

    [McpServerTool]
    [Description("""
                 为主题添加标记(如优先级、任务状态、旗帜等)。

                 ⚠️ 重要: topicId 必须是主题的 ID(GUID),不是标题!

                 正确使用流程: 
                 1. 先用 SearchTopicsByTitle 搜索主题
                 2. 从返回结果中获取 id 字段值
                 3. 调用此工具传入该 id、标记组和标记ID
                 """)]
    public static async Task<string> AddMarkerToTopic(
        [Description("XMind 文件的完整路径")]
        string filePath,
        [Description("主题 ID(必须是 SearchTopicsByTitle 返回的 id 字段,不是标题!)")]
        string topicId,
        [Description("标记组 ID(例如: priorityMarkers, taskMarkers, flagMarkers)")]
        string groupId,
        [Description("标记 ID(例如: priority-1, task-done, flag-red)")]
        string markerId,
        [Description("工作表标题(可选,默认使用第一个工作表)")]
        string? sheetTitle = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var doc = await XmindReader.LoadAsync(filePath, cancellationToken);
            var sheet = GetTargetSheet(doc, sheetTitle);
            if (sheet == null)
            {
                return MissingSheet(sheetTitle);
            }
            var topic = FindTopic(sheet, topicId);
            if (topic == null)
            {
                return ToolJson.Error($"Topic ID '{topicId}' not found. Use SearchTopicsByTitle first to get the correct topic ID.");
            }
            var marker = new Marker { GroupId = groupId, MarkerId = markerId };
            TopicEditor.AddMarker(topic, marker);
            await XmindWriter.SaveAsync(doc, cancellationToken: cancellationToken);
            return ToolJson.Serialize(new
            {
                success = true,
                sheetTitle = sheet.Title,
                topicId,
                topicTitle = topic.Title,
                marker = new { groupId, markerId },
                message = "Marker added"
            });
        }
        catch (Exception ex)
        {
            return ToolJson.Error(ex.Message);
        }
    }

    [McpServerTool]
    [Description("""
                 从指定主题移除标记。

                 ⚠️ 重要: topicId 必须是主题的 ID(GUID),不是标题!

                 正确使用流程: 
                 1. 先用 SearchTopicsByTitle 搜索主题
                 2. 从返回结果中获取 id 字段值
                 3. 调用此工具传入该 id 和要移除的标记ID
                 """)]
    public static async Task<string> RemoveMarkerFromTopic(
        [Description("XMind 文件的完整路径")]
        string filePath,
        [Description("主题 ID(必须是 SearchTopicsByTitle 返回的 id 字段,不是标题!)")]
        string topicId,
        [Description("标记 ID")]
        string markerId,
        [Description("工作表标题(可选,默认使用第一个工作表)")]
        string? sheetTitle = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var doc = await XmindReader.LoadAsync(filePath, cancellationToken);
            var sheet = GetTargetSheet(doc, sheetTitle);
            if (sheet == null)
            {
                return MissingSheet(sheetTitle);
            }
            var topic = FindTopic(sheet, topicId);
            if (topic == null)
            {
                return ToolJson.Error($"Topic ID '{topicId}' not found. Use SearchTopicsByTitle first to get the correct topic ID.");
            }
            if (!TopicEditor.RemoveMarker(topic, markerId))
            {
                return ToolJson.Error($"Marker '{markerId}' not found on this topic. Use GetTopicDetails to see the markers on this topic.");
            }
            await XmindWriter.SaveAsync(doc, cancellationToken: cancellationToken);
            return ToolJson.Serialize(new
            {
                success = true,
                sheetTitle = sheet.Title,
                topicId,
                markerId,
                message = "Marker removed"
            });
        }
        catch (Exception ex)
        {
            return ToolJson.Error(ex.Message);
        }
    }

    [McpServerTool]
    [Description("""
                 为主题添加标签。

                 ⚠️ 重要: topicId 必须是主题的 ID(GUID),不是标题!

                 正确使用流程: 
                 1. 先用 SearchTopicsByTitle 搜索主题
                 2. 从返回结果中获取 id 字段值
                 3. 调用此工具传入该 id 和标签内容
                 """)]
    public static async Task<string> AddLabelToTopic(
        [Description("XMind 文件的完整路径")]
        string filePath,
        [Description("主题 ID(必须是 SearchTopicsByTitle 返回的 id 字段,不是标题!)")]
        string topicId,
        [Description("标签内容")]
        string label,
        [Description("工作表标题(可选,默认使用第一个工作表)")]
        string? sheetTitle = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var doc = await XmindReader.LoadAsync(filePath, cancellationToken);
            var sheet = GetTargetSheet(doc, sheetTitle);
            if (sheet == null)
            {
                return MissingSheet(sheetTitle);
            }
            var topic = FindTopic(sheet, topicId);
            if (topic == null)
            {
                return ToolJson.Error($"Topic ID '{topicId}' not found. Use SearchTopicsByTitle first to get the correct topic ID.");
            }
            TopicEditor.AddLabel(topic, label);
            await XmindWriter.SaveAsync(doc, cancellationToken: cancellationToken);
            return ToolJson.Serialize(new
            {
                success = true,
                sheetTitle = sheet.Title,
                topicId,
                topicTitle = topic.Title,
                label,
                message = "Label added"
            });
        }
        catch (Exception ex)
        {
            return ToolJson.Error(ex.Message);
        }
    }

    [McpServerTool]
    [Description("""
                 从指定主题移除标签。

                 ⚠️ 重要: topicId 必须是主题的 ID(GUID),不是标题!

                 正确使用流程: 
                 1. 先用 SearchTopicsByTitle 搜索主题
                 2. 从返回结果中获取 id 字段值
                 3. 调用此工具传入该 id 和要移除的标签
                 """)]
    public static async Task<string> RemoveLabelFromTopic(
        [Description("XMind 文件的完整路径")]
        string filePath,
        [Description("主题 ID(必须是 SearchTopicsByTitle 返回的 id 字段,不是标题!)")]
        string topicId,
        [Description("标签内容")]
        string label,
        [Description("工作表标题(可选,默认使用第一个工作表)")]
        string? sheetTitle = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var doc = await XmindReader.LoadAsync(filePath, cancellationToken);
            var sheet = GetTargetSheet(doc, sheetTitle);
            if (sheet == null)
            {
                return MissingSheet(sheetTitle);
            }
            var topic = FindTopic(sheet, topicId);
            if (topic == null)
            {
                return ToolJson.Error($"Topic ID '{topicId}' not found. Use SearchTopicsByTitle first to get the correct topic ID.");
            }
            if (!TopicEditor.RemoveLabel(topic, label))
            {
                return ToolJson.Error($"Label '{label}' not found on this topic. Use GetTopicDetails to see the labels on this topic.");
            }
            await XmindWriter.SaveAsync(doc, cancellationToken: cancellationToken);
            return ToolJson.Serialize(new
            {
                success = true,
                sheetTitle = sheet.Title,
                topicId,
                label,
                message = "Label removed"
            });
        }
        catch (Exception ex)
        {
            return ToolJson.Error(ex.Message);
        }
    }

    [McpServerTool]
    [Description("""
                 为指定主题设置外部链接。

                 ⚠️ 重要: topicId 必须是主题的 ID(GUID),不是标题!

                 正确使用流程: 
                 1. 先用 SearchTopicsByTitle 搜索主题
                 2. 从返回结果中获取 id 字段值
                 3. 调用此工具传入该 id 和链接地址
                 """)]
    public static async Task<string> SetTopicLink(
        [Description("XMind 文件的完整路径")]
        string filePath,
        [Description("主题 ID(必须是 SearchTopicsByTitle 返回的 id 字段,不是标题!)")]
        string topicId,
        [Description("链接地址")]
        string url,
        [Description("工作表标题(可选,默认使用第一个工作表)")]
        string? sheetTitle = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var doc = await XmindReader.LoadAsync(filePath, cancellationToken);
            var sheet = GetTargetSheet(doc, sheetTitle);
            if (sheet == null)
            {
                return MissingSheet(sheetTitle);
            }
            var topic = FindTopic(sheet, topicId);
            if (topic == null)
            {
                return ToolJson.Error($"Topic ID '{topicId}' not found. Use SearchTopicsByTitle first to get the correct topic ID.");
            }
            TopicEditor.SetLink(topic, url);
            await XmindWriter.SaveAsync(doc, cancellationToken: cancellationToken);
            return ToolJson.Serialize(new
            {
                success = true,
                sheetTitle = sheet.Title,
                topicId,
                url,
                message = "Topic link updated"
            });
        }
        catch (Exception ex)
        {
            return ToolJson.Error(ex.Message);
        }
    }

    [McpServerTool]
    [Description("""
                 清除指定主题的链接。

                 ⚠️ 重要: topicId 必须是主题的 ID(GUID),不是标题!

                 正确使用流程: 
                 1. 先用 SearchTopicsByTitle 搜索主题
                 2. 从返回结果中获取 id 字段值
                 3. 调用此工具传入该 id
                 """)]
    public static async Task<string> ClearTopicLink(
        [Description("XMind 文件的完整路径")]
        string filePath,
        [Description("主题 ID(必须是 SearchTopicsByTitle 返回的 id 字段,不是标题!)")]
        string topicId,
        [Description("工作表标题(可选,默认使用第一个工作表)")]
        string? sheetTitle = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var doc = await XmindReader.LoadAsync(filePath, cancellationToken);
            var sheet = GetTargetSheet(doc, sheetTitle);
            if (sheet == null)
            {
                return MissingSheet(sheetTitle);
            }
            var topic = FindTopic(sheet, topicId);
            if (topic == null)
            {
                return ToolJson.Error($"Topic ID '{topicId}' not found. Use SearchTopicsByTitle first to get the correct topic ID.");
            }
            TopicEditor.ClearLink(topic);
            await XmindWriter.SaveAsync(doc, cancellationToken: cancellationToken);
            return ToolJson.Serialize(new
            {
                success = true,
                sheetTitle = sheet.Title,
                topicId,
                message = "Topic link cleared"
            });
        }
        catch (Exception ex)
        {
            return ToolJson.Error(ex.Message);
        }
    }

    [McpServerTool]
    [Description("""
                 移动主题到新的父主题下。

                 ⚠️ 重要: topicId 和 newParentTopicId 都必须是主题的 ID(GUID),不是标题!

                 正确使用流程: 
                 1. 先用 SearchTopicsByTitle 搜索要移动的主题,获取其 id
                 2. 再搜索新的父主题,获取其 id
                 3. 调用此工具传入两个 id 值
                 """)]
    public static async Task<string> MoveTopic(
        [Description("XMind 文件的完整路径")]
        string filePath,
        [Description("要移动的主题 ID(必须是 SearchTopicsByTitle 返回的 id 字段,不是标题!)")]
        string topicId,
        [Description("新的父主题 ID(必须是 SearchTopicsByTitle 返回的 id 字段,不是标题!)")]
        string newParentTopicId,
        [Description("目标工作表标题(可选,默认使用第一个工作表)")]
        string? sheetTitle = null,
        [Description("插入位置(可选)")]
        int? index = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var doc = await XmindReader.LoadAsync(filePath, cancellationToken);
            var sheet = GetTargetSheet(doc, sheetTitle);
            if (sheet == null)
            {
                return MissingSheet(sheetTitle);
            }
            var topic = FindTopic(sheet, topicId);
            if (topic == null)
            {
                return ToolJson.Error($"Topic ID '{topicId}' not found. Use SearchTopicsByTitle first to get the correct topic ID.");
            }
            if (topic == sheet.RootTopic)
            {
                return ToolJson.Error("Cannot move root topic");
            }
            var newParent = FindTopic(sheet, newParentTopicId);
            if (newParent == null)
            {
                return ToolJson.Error($"New parent topic ID '{newParentTopicId}' not found. Use SearchTopicsByTitle first to get the correct topic ID.");
            }
            if (index.HasValue)
            {
                TopicEditor.MoveTopicToPosition(topic, newParent, index.Value);
            }
            else
            {
                TopicEditor.MoveTopic(topic, newParent);
            }
            await XmindWriter.SaveAsync(doc, cancellationToken: cancellationToken);
            return ToolJson.Serialize(new
            {
                success = true,
                sheetTitle = sheet.Title,
                topicId,
                newParentTopicId,
                index,
                message = "Topic moved"
            });
        }
        catch (Exception ex)
        {
            return ToolJson.Error(ex.Message);
        }
    }

    [McpServerTool]
    [Description("""
                 复制主题到新的父主题下。

                 ⚠️ 重要: topicId 和 parentTopicId 都必须是主题的 ID(GUID),不是标题!

                 正确使用流程: 
                 1. 先用 SearchTopicsByTitle 搜索要复制的主题,获取其 id
                 2. 再搜索目标父主题,获取其 id
                 3. 调用此工具传入两个 id 值
                 """)]
    public static async Task<string> CloneTopicTool(
        [Description("XMind 文件的完整路径")]
        string filePath,
        [Description("要复制的主题 ID(必须是 SearchTopicsByTitle 返回的 id 字段,不是标题!)")]
        string topicId,
        [Description("目标父主题 ID(必须是 SearchTopicsByTitle 返回的 id 字段,不是标题!)")]
        string parentTopicId,
        [Description("工作表标题(可选,默认使用第一个工作表)")]
        string? sheetTitle = null,
        [Description("插入位置(可选)")]
        int? index = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var doc = await XmindReader.LoadAsync(filePath, cancellationToken);
            var sheet = GetTargetSheet(doc, sheetTitle);
            if (sheet == null)
            {
                return MissingSheet(sheetTitle);
            }
            var source = FindTopic(sheet, topicId);
            if (source == null)
            {
                return ToolJson.Error($"Topic ID '{topicId}' not found. Use SearchTopicsByTitle first to get the correct topic ID.");
            }
            var parent = FindTopic(sheet, parentTopicId);
            if (parent == null)
            {
                return ToolJson.Error($"Parent topic ID '{parentTopicId}' not found. Use SearchTopicsByTitle first to get the correct topic ID.");
            }
            var clone = TopicEditor.CloneTopic(source, parent);
            parent.Children ??= new();
            parent.Children.Attached ??= [];
            if (index.HasValue)
            {
                var actualIndex = Math.Clamp(index.Value, 0, parent.Children.Attached.Count);
                parent.Children.Attached.Insert(actualIndex, clone);
            }
            else
            {
                parent.Children.Attached.Add(clone);
            }
            await XmindWriter.SaveAsync(doc, cancellationToken: cancellationToken);
            return ToolJson.Serialize(new
            {
                success = true,
                sheetTitle = sheet.Title,
                sourceTopicId = topicId,
                clonedTopic = new
                {
                    id = clone.Id,
                    title = clone.Title,
                    parentTopicId = parent.Id
                },
                message = "Topic cloned"
            });
        }
        catch (Exception ex)
        {
            return ToolJson.Error(ex.Message);
        }
    }

    [McpServerTool]
    [Description("""
                 删除指定主题(不能删除根主题)。

                 ⚠️ 重要: topicId 必须是主题的 ID(GUID),不是标题!

                 正确使用流程: 
                 1. 先用 SearchTopicsByTitle 搜索主题
                 2. 从返回结果中获取 id 字段值
                 3. 调用此工具传入该 id
                 """)]
    public static async Task<string> DeleteTopic(
        [Description("XMind 文件的完整路径")]
        string filePath,
        [Description("主题 ID(必须是 SearchTopicsByTitle 返回的 id 字段,不是标题!)")]
        string topicId,
        [Description("工作表标题(可选,默认使用第一个工作表)")]
        string? sheetTitle = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var doc = await XmindReader.LoadAsync(filePath, cancellationToken);
            var sheet = GetTargetSheet(doc, sheetTitle);
            if (sheet == null)
            {
                return MissingSheet(sheetTitle);
            }
            var topic = FindTopic(sheet, topicId);
            if (topic == null)
            {
                return ToolJson.Error($"Topic ID '{topicId}' not found. Use SearchTopicsByTitle first to get the correct topic ID.");
            }
            if (topic == sheet.RootTopic)
            {
                return ToolJson.Error("Cannot delete root topic");
            }
            var deletedTitle = topic.Title;
            TopicEditor.RemoveTopic(topic);
            await XmindWriter.SaveAsync(doc, cancellationToken: cancellationToken);
            return ToolJson.Serialize(new
            {
                success = true,
                sheetTitle = sheet.Title,
                deletedTopicId = topicId,
                deletedTitle,
                message = "Topic deleted"
            });
        }
        catch (Exception ex)
        {
            return ToolJson.Error(ex.Message);
        }
    }

    [McpServerTool]
    [Description("新增工作表")]
    public static async Task<string> AddSheet(
        [Description("XMind 文件的完整路径")]
        string filePath,
        [Description("工作表标题")]
        string sheetTitle,
        [Description("根主题标题")]
        string rootTitle,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var doc = await XmindReader.LoadAsync(filePath, cancellationToken);
            var sheet = doc.AddSheet(sheetTitle, rootTitle);
            await XmindWriter.SaveAsync(doc, cancellationToken: cancellationToken);
            return ToolJson.Serialize(new
            {
                success = true,
                sheetId = sheet.Id,
                sheetTitle = sheet.Title,
                rootTopicId = sheet.RootTopic.Id,
                message = "Sheet added"
            });
        }
        catch (Exception ex)
        {
            return ToolJson.Error(ex.Message);
        }
    }

    [McpServerTool]
    [Description("重命名工作表")]
    public static async Task<string> RenameSheet(
        [Description("XMind 文件的完整路径")]
        string filePath,
        [Description("当前工作表标题")]
        string currentTitle,
        [Description("新的工作表标题")]
        string newTitle,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var doc = await XmindReader.LoadAsync(filePath, cancellationToken);
            if (!doc.RenameSheet(currentTitle, newTitle))
            {
                return ToolJson.Error($"Sheet '{currentTitle}' not found");
            }
            await XmindWriter.SaveAsync(doc, cancellationToken: cancellationToken);
            return ToolJson.Serialize(new
            {
                success = true,
                oldTitle = currentTitle,
                newTitle,
                message = "Sheet renamed"
            });
        }
        catch (Exception ex)
        {
            return ToolJson.Error(ex.Message);
        }
    }

    [McpServerTool]
    [Description("删除工作表")]
    public static async Task<string> DeleteSheet(
        [Description("XMind 文件的完整路径")]
        string filePath,
        [Description("工作表标题")]
        string sheetTitle,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var doc = await XmindReader.LoadAsync(filePath, cancellationToken);
            if (doc.Sheets.Count <= 1)
            {
                return ToolJson.Error("Cannot delete the last sheet");
            }
            if (!doc.RemoveSheet(sheetTitle))
            {
                return ToolJson.Error($"Sheet '{sheetTitle}' not found");
            }
            await XmindWriter.SaveAsync(doc, cancellationToken: cancellationToken);
            return ToolJson.Serialize(new
            {
                success = true,
                sheetTitle,
                message = "Sheet deleted"
            });
        }
        catch (Exception ex)
        {
            return ToolJson.Error(ex.Message);
        }
    }

    [McpServerTool]
    [Description("""
                 添加两个主题之间的关系(连接线)。

                 ⚠️ 重要: end1TopicId 和 end2TopicId 必须是主题的 ID(GUID),不是标题!

                 正确使用流程: 
                 1. 先用 SearchTopicsByTitle 搜索主题标题(如 "ML.NET"),获取其 id 字段
                 2. 再搜索另一个主题(如 "Azure Machine Learning"),获取其 id 字段
                 3. 最后调用此工具传入两个 id 值

                 示例: end1TopicId="abc123-def456", end2TopicId="789ghi-012jkl"
                 """)]
    public static async Task<string> AddRelationship(
        [Description("XMind 文件的完整路径")]
        string filePath,
        [Description("起始主题 ID(必须是 SearchTopicsByTitle 返回的 id 字段,不是标题!)")]
        string end1TopicId,
        [Description("结束主题 ID(必须是 SearchTopicsByTitle 返回的 id 字段,不是标题!)")]
        string end2TopicId,
        [Description("工作表标题(可选,默认使用第一个工作表)")]
        string? sheetTitle = null,
        [Description("关系标题(可选)")]
        string? title = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var doc = await XmindReader.LoadAsync(filePath, cancellationToken);
            var sheet = GetTargetSheet(doc, sheetTitle);
            if (sheet == null)
            {
                return MissingSheet(sheetTitle);
            }
            var topics = TopicSearchEngine.GetAllTopics(sheet);
            if (topics.All(t => t.Id != end1TopicId) || topics.All(t => t.Id != end2TopicId))
            {
                return ToolJson.Error($"Topic ID not found. You must use SearchTopicsByTitle first to get topic IDs. Received end1TopicId='{end1TopicId}', end2TopicId='{end2TopicId}'. Note: Topic IDs are GUIDs (e.g., 'abc123-def456'), NOT topic titles (e.g., 'ML.NET').");
            }
            sheet.Relationships ??= [];
            var relationship = new Relationship
            {
                End1Id = end1TopicId,
                End2Id = end2TopicId,
                Title = title
            };
            sheet.Relationships.Add(relationship);
            await XmindWriter.SaveAsync(doc, cancellationToken: cancellationToken);
            return ToolJson.Serialize(new
            {
                success = true,
                sheetTitle = sheet.Title,
                relationship = new { id = relationship.Id, end1TopicId, end2TopicId, title },
                message = "Relationship added"
            });
        }
        catch (Exception ex)
        {
            return ToolJson.Error(ex.Message);
        }
    }

    [McpServerTool]
    [Description("""
                 移除指定的关系(连接线)。

                 ⚠️ 重要: relationshipId 必须是关系的 ID(GUID),不是关系标题!

                 正确使用流程: 
                 1. 先用 ListRelationships 查看所有关系及其 id
                 2. 找到要删除的关系的 id 字段值
                 3. 调用此工具传入该 id
                 """)]
    public static async Task<string> RemoveRelationship(
        [Description("XMind 文件的完整路径")]
        string filePath,
        [Description("关系 ID(必须是 ListRelationships 返回的 id 字段,不是标题!)")]
        string relationshipId,
        [Description("工作表标题(可选,默认使用第一个工作表)")]
        string? sheetTitle = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var doc = await XmindReader.LoadAsync(filePath, cancellationToken);
            var sheet = GetTargetSheet(doc, sheetTitle);
            if (sheet == null)
            {
                return MissingSheet(sheetTitle);
            }
            var relationship = sheet.Relationships?.FirstOrDefault(r => r.Id == relationshipId);
            if (relationship == null)
            {
                return ToolJson.Error($"Relationship ID '{relationshipId}' not found. Use ListRelationships first to get the correct relationship ID.");
            }
            sheet.Relationships!.Remove(relationship);
            await XmindWriter.SaveAsync(doc, cancellationToken: cancellationToken);
            return ToolJson.Serialize(new
            {
                success = true,
                sheetTitle = sheet.Title,
                relationshipId,
                message = "Relationship removed"
            });
        }
        catch (Exception ex)
        {
            return ToolJson.Error(ex.Message);
        }
    }

    private static Sheet? GetTargetSheet(XmindDocument doc, string? sheetTitle) => sheetTitle != null ? doc.FindSheet(sheetTitle) : doc.GetActiveSheet();

    private static Topic? FindTopic(Sheet sheet, string topicId) => TopicSearchEngine.GetAllTopics(sheet).FirstOrDefault(t => t.Id == topicId);

    private static string MissingSheet(string? sheetTitle) => ToolJson.Error(sheetTitle == null ? "No sheets found" : $"Sheet '{sheetTitle}' not found");
}