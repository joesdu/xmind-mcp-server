using System.Text.Json.Serialization;

namespace XmindMcp.Server.Models;

/// <summary>
/// XMind 主题节点
/// </summary>
public class Topic
{
    /// <summary>
    /// 子主题容器
    /// </summary>
    [JsonPropertyName("children")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public TopicChildren? Children { get; set; }

    /// <summary>
    /// 链接
    /// </summary>
    [JsonPropertyName("href")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Href { get; set; }

    /// <summary>
    /// 主题唯一标识符
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// 主题标签
    /// </summary>
    [JsonPropertyName("labels")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? Labels { get; set; }

    /// <summary>
    /// 主题标记
    /// </summary>
    [JsonPropertyName("markers")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<Marker>? Markers { get; set; }

    /// <summary>
    /// 主题备注
    /// </summary>
    [JsonPropertyName("notes")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public TopicNotes? Notes { get; set; }

    /// <summary>
    /// 父节点引用（不序列化）
    /// </summary>
    [JsonIgnore]
    public Topic? Parent { get; set; }

    /// <summary>
    /// 获取节点路径
    /// </summary>
    [JsonIgnore]
    public string Path
    {
        get
        {
            var path = new List<string>();
            var current = this;
            while (current != null)
            {
                path.Insert(0, current.Title);
                current = current.Parent;
            }
            return string.Join(" → ", path);
        }
    }

    /// <summary>
    /// 主题标题
    /// </summary>
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;
}

/// <summary>
/// 主题备注
/// </summary>
public class TopicNotes
{
    [JsonPropertyName("plain")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public PlainNote? Plain { get; set; }
}

/// <summary>
/// 纯文本备注
/// </summary>
public class PlainNote
{
    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;
}

/// <summary>
/// 主题子节点容器
/// </summary>
public class TopicChildren
{
    [JsonPropertyName("attached")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<Topic>? Attached { get; set; }
}