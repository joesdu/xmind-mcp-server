using System.Text.RegularExpressions;
using XmindMcp.Models;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedMember.Global

namespace XmindMcp.Services;

/// <summary>
/// 主题搜索引擎
/// </summary>
public class TopicSearchEngine
{
    /// <summary>
    /// 按标题搜索
    /// </summary>
    /// <param name="sheet">工作表</param>
    /// <param name="keyword">关键词</param>
    /// <param name="caseSensitive">是否区分大小写</param>
    /// <returns>匹配的主题列表</returns>
    public static List<Topic> FindByTitle(Sheet sheet, string keyword, bool caseSensitive = false)
    {
        var results = new List<Topic>();
        SearchRecursive(sheet.RootTopic, keyword, caseSensitive, results);
        return results;
    }

    /// <summary>
    /// 使用正则表达式搜索标题
    /// </summary>
    public static List<Topic> FindByTitleRegex(Sheet sheet, string pattern, RegexOptions options = RegexOptions.None) => FindByTitleRegex(sheet, new(pattern, options));

    /// <summary>
    /// 使用已编译正则表达式搜索标题
    /// </summary>
    public static List<Topic> FindByTitleRegex(Sheet sheet, Regex regex)
    {
        var results = new List<Topic>();
        SearchByRegexRecursive(sheet.RootTopic, regex, results);
        return results;
    }

    /// <summary>
    /// 按标记搜索
    /// </summary>
    public static List<Topic> FindByMarker(Sheet sheet, string markerId)
    {
        var results = new List<Topic>();
        SearchByMarkerRecursive(sheet.RootTopic, markerId, results);
        return results;
    }

    /// <summary>
    /// 按标签搜索
    /// </summary>
    public static List<Topic> FindByLabel(Sheet sheet, string label)
    {
        var results = new List<Topic>();
        SearchByLabelRecursive(sheet.RootTopic, label, results);
        return results;
    }

    /// <summary>
    /// 按备注内容搜索
    /// </summary>
    public static List<Topic> FindByNote(Sheet sheet, string keyword)
    {
        var results = new List<Topic>();
        SearchByNoteRecursive(sheet.RootTopic, keyword, results);
        return results;
    }

    /// <summary>
    /// 获取所有叶子节点
    /// </summary>
    public static List<Topic> GetLeafNodes(Topic topic)
    {
        if (topic.Children?.Attached == null || topic.Children.Attached.Count == 0)
        {
            return [topic];
        }
        return topic.Children.Attached.SelectMany(GetLeafNodes).ToList();
    }

    /// <summary>
    /// 获取所有节点（扁平化）
    /// </summary>
    public static List<Topic> GetAllTopics(Sheet sheet)
    {
        var results = new List<Topic>();
        FlattenTopics(sheet.RootTopic, results);
        return results;
    }

    /// <summary>
    /// 获取节点深度
    /// </summary>
    public static int GetDepth(Topic topic)
    {
        if (topic.Children?.Attached == null || topic.Children.Attached.Count == 0)
        {
            return 0;
        }
        return 1 + topic.Children.Attached.Max(GetDepth);
    }

    /// <summary>
    /// 统计节点数量
    /// </summary>
    public static int CountTopics(Sheet sheet) => CountRecursive(sheet.RootTopic);

    /// <summary>
    /// 查找父节点
    /// </summary>
    public static Topic? FindParent(Topic topic, Func<Topic, bool> predicate)
    {
        var current = topic.Parent;
        while (current != null)
        {
            if (predicate(current))
            {
                return current;
            }
            current = current.Parent;
        }
        return null;
    }

    /// <summary>
    /// 获取兄弟节点
    /// </summary>
    public static List<Topic> GetSiblings(Topic topic)
    {
        if (topic.Parent?.Children?.Attached == null)
        {
            return [];
        }
        return topic.Parent.Children.Attached
                    .Where(t => t.Id != topic.Id)
                    .ToList();
    }

    /// <summary>
    /// 获取节点路径（ID 列表）
    /// </summary>
    public static List<string> GetPathIds(Topic topic)
    {
        var path = new List<string>();
        var current = topic;
        while (current != null)
        {
            path.Insert(0, current.Id);
            current = current.Parent;
        }
        return path;
    }

    // 私有递归方法

    private static void SearchRecursive(Topic topic, string keyword, bool caseSensitive, List<Topic> results)
    {
        var comparison = caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
        if (topic.Title.Contains(keyword, comparison))
        {
            results.Add(topic);
        }
        if (topic.Children?.Attached == null)
        {
            return;
        }
        foreach (var child in topic.Children.Attached)
        {
            SearchRecursive(child, keyword, caseSensitive, results);
        }
    }

    private static void SearchByRegexRecursive(Topic topic, Regex regex, List<Topic> results)
    {
        if (regex.IsMatch(topic.Title))
        {
            results.Add(topic);
        }
        if (topic.Children?.Attached == null)
        {
            return;
        }
        foreach (var child in topic.Children.Attached)
        {
            SearchByRegexRecursive(child, regex, results);
        }
    }

    private static void SearchByMarkerRecursive(Topic topic, string markerId, List<Topic> results)
    {
        if (topic.Markers != null && topic.Markers.Any(m => m.MarkerId == markerId))
        {
            results.Add(topic);
        }
        if (topic.Children?.Attached == null)
        {
            return;
        }
        foreach (var child in topic.Children.Attached)
        {
            SearchByMarkerRecursive(child, markerId, results);
        }
    }

    private static void SearchByLabelRecursive(Topic topic, string label, List<Topic> results)
    {
        if (topic.Labels != null && topic.Labels.Contains(label))
        {
            results.Add(topic);
        }
        if (topic.Children?.Attached == null)
        {
            return;
        }
        foreach (var child in topic.Children.Attached)
        {
            SearchByLabelRecursive(child, label, results);
        }
    }

    private static void SearchByNoteRecursive(Topic topic, string keyword, List<Topic> results)
    {
        if (topic.Notes?.Plain?.Content.Contains(keyword, StringComparison.OrdinalIgnoreCase) == true)
        {
            results.Add(topic);
        }
        if (topic.Children?.Attached == null)
        {
            return;
        }
        foreach (var child in topic.Children.Attached)
        {
            SearchByNoteRecursive(child, keyword, results);
        }
    }

    private static void FlattenTopics(Topic topic, List<Topic> results)
    {
        results.Add(topic);
        if (topic.Children?.Attached == null)
        {
            return;
        }
        foreach (var child in topic.Children.Attached)
        {
            FlattenTopics(child, results);
        }
    }

    private static int CountRecursive(Topic topic)
    {
        var count = 1;
        if (topic.Children?.Attached != null)
        {
            count += topic.Children.Attached.Sum(CountRecursive);
        }
        return count;
    }
}