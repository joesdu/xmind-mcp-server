using XmindMcp.Server.Models;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedMember.Global

namespace XmindMcp.Server.Services;

/// <summary>
/// 主题编辑器
/// </summary>
public class TopicEditor
{
    /// <summary>
    /// 添加子主题
    /// </summary>
    public static Topic AddChild(Topic parent, string title)
    {
        var child = new Topic
        {
            Title = title,
            Parent = parent
        };
        parent.Children ??= new();
        parent.Children.Attached ??= [];
        parent.Children.Attached.Add(child);
        return child;
    }

    /// <summary>
    /// 批量添加子主题
    /// </summary>
    public static List<Topic> AddChildren(Topic parent, IEnumerable<string> titles)
    {
        return titles.Select(t => AddChild(parent, t)).ToList();
    }

    /// <summary>
    /// 插入子主题到指定位置
    /// </summary>
    public static Topic InsertChild(Topic parent, int index, string title)
    {
        var child = new Topic
        {
            Title = title,
            Parent = parent
        };
        parent.Children ??= new();
        parent.Children.Attached ??= [];
        index = Math.Clamp(index, 0, parent.Children.Attached.Count);
        parent.Children.Attached.Insert(index, child);
        return child;
    }

    /// <summary>
    /// 移动主题到新的父节点
    /// </summary>
    public static void MoveTopic(Topic topic, Topic newParent)
    {
        topic.Parent?.Children?.Attached?.Remove(topic);
        topic.Parent = newParent;
        newParent.Children ??= new();
        newParent.Children.Attached ??= [];
        newParent.Children.Attached.Add(topic);
    }

    /// <summary>
    /// 移动主题到指定位置
    /// </summary>
    public static void MoveTopicToPosition(Topic topic, Topic newParent, int index)
    {
        topic.Parent?.Children?.Attached?.Remove(topic);
        topic.Parent = newParent;
        newParent.Children ??= new();
        newParent.Children.Attached ??= [];
        index = Math.Clamp(index, 0, newParent.Children.Attached.Count);
        newParent.Children.Attached.Insert(index, topic);
    }

    /// <summary>
    /// 删除主题
    /// </summary>
    public static bool RemoveTopic(Topic topic)
    {
        if (topic.Parent?.Children?.Attached == null)
        {
            return false;
        }
        return topic.Parent.Children.Attached.Remove(topic);
    }

    /// <summary>
    /// 复制主题（深拷贝）
    /// </summary>
    public static Topic CloneTopic(Topic source, Topic? newParent = null)
    {
        var clone = new Topic
        {
            Id = Guid.NewGuid().ToString(),
            Title = source.Title,
            Notes = source.Notes != null
                        ? new TopicNotes { Plain = new() { Content = source.Notes.Plain?.Content ?? string.Empty } }
                        : null,
            Labels = source.Labels != null ? [..source.Labels] : null,
            Markers = source.Markers != null ? [..source.Markers] : null,
            Href = source.Href,
            Parent = newParent
        };
        if (source.Children?.Attached != null)
        {
            clone.Children = new()
            {
                Attached = source.Children.Attached
                                 .Select(c => CloneTopic(c, clone))
                                 .ToList()
            };
        }
        return clone;
    }

    /// <summary>
    /// 更新主题标题
    /// </summary>
    public static void UpdateTitle(Topic topic, string newTitle)
    {
        topic.Title = newTitle;
    }

    /// <summary>
    /// 更新主题备注
    /// </summary>
    public static void UpdateNotes(Topic topic, string notes)
    {
        topic.Notes = new()
        {
            Plain = new() { Content = notes }
        };
    }

    /// <summary>
    /// 清除主题备注
    /// </summary>
    public static void ClearNotes(Topic topic)
    {
        topic.Notes = null;
    }

    /// <summary>
    /// 添加标记
    /// </summary>
    public static void AddMarker(Topic topic, Marker marker)
    {
        topic.Markers ??= [];
        if (topic.Markers.All(m => m.MarkerId != marker.MarkerId))
        {
            topic.Markers.Add(marker);
        }
    }

    /// <summary>
    /// 移除标记
    /// </summary>
    public static bool RemoveMarker(Topic topic, string markerId)
    {
        var marker = topic.Markers?.FirstOrDefault(m => m.MarkerId == markerId);
        if (marker == null)
        {
            return false;
        }
        topic.Markers?.Remove(marker);
        return true;
    }

    /// <summary>
    /// 添加标签
    /// </summary>
    public static void AddLabel(Topic topic, string label)
    {
        topic.Labels ??= [];
        if (!topic.Labels.Contains(label))
        {
            topic.Labels.Add(label);
        }
    }

    /// <summary>
    /// 移除标签
    /// </summary>
    public static bool RemoveLabel(Topic topic, string label) => topic.Labels != null && topic.Labels.Remove(label);

    /// <summary>
    /// 设置链接
    /// </summary>
    public static void SetLink(Topic topic, string url)
    {
        topic.Href = url;
    }

    /// <summary>
    /// 清除链接
    /// </summary>
    public static void ClearLink(Topic topic)
    {
        topic.Href = null;
    }

    /// <summary>
    /// 批量重命名
    /// </summary>
    public static void RenameAll(Sheet sheet, Func<string, string> transform)
    {
        RenameRecursive(sheet.RootTopic, transform);
    }

    /// <summary>
    /// 批量添加标记
    /// </summary>
    public static void AddMarkerToAll(Sheet sheet, Marker marker)
    {
        AddMarkerRecursive(sheet.RootTopic, marker);
    }

    private static void RenameRecursive(Topic topic, Func<string, string> transform)
    {
        topic.Title = transform(topic.Title);
        if (topic.Children?.Attached == null)
        {
            return;
        }
        foreach (var child in topic.Children.Attached)
        {
            RenameRecursive(child, transform);
        }
    }

    private static void AddMarkerRecursive(Topic topic, Marker marker)
    {
        AddMarker(topic, marker);
        if (topic.Children?.Attached == null)
        {
            return;
        }
        foreach (var child in topic.Children.Attached)
        {
            AddMarkerRecursive(child, marker);
        }
    }
}