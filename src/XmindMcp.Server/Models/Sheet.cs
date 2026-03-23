using System.Text.Json.Serialization;

// ReSharper disable UnusedMember.Global

namespace XmindMcp.Server.Models;

/// <summary>
/// XMind 工作表
/// </summary>
public class Sheet
{
    /// <summary>
    /// 工作表唯一标识符
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// 关系列表
    /// </summary>
    [JsonPropertyName("relationships")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<Relationship>? Relationships { get; set; }

    /// <summary>
    /// 根主题
    /// </summary>
    [JsonPropertyName("rootTopic")]
    public Topic RootTopic { get; set; } = new();

    /// <summary>
    /// 主题
    /// </summary>
    [JsonPropertyName("theme")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public SheetTheme? Theme { get; set; }

    /// <summary>
    /// 工作表标题
    /// </summary>
    [JsonPropertyName("sheetTitle")]
    public string Title { get; set; } = "Sheet 1";

    /// <summary>
    /// 主题定位方式
    /// </summary>
    [JsonPropertyName("topicPositioning")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? TopicPositioning { get; set; }
}

/// <summary>
/// 工作表主题
/// </summary>
public class SheetTheme
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [JsonPropertyName("title")]
    public string Title { get; set; } = "robust";
}