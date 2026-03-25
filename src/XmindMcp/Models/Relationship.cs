using System.Text.Json.Serialization;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedMember.Global

namespace XmindMcp.Models;

/// <summary>
/// XMind 关系（连接线）
/// </summary>
public class Relationship
{
    /// <summary>
    /// 控制点
    /// </summary>
    [JsonPropertyName("controlPoints")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, ControlPoint>? ControlPoints { get; set; }

    /// <summary>
    /// 起始主题 ID
    /// </summary>
    [JsonPropertyName("end1Id")]
    public string End1Id { get; set; } = string.Empty;

    /// <summary>
    /// 目标主题 ID
    /// </summary>
    [JsonPropertyName("end2Id")]
    public string End2Id { get; set; } = string.Empty;

    /// <summary>
    /// 关系唯一标识符
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// 关系标题
    /// </summary>
    [JsonPropertyName("title")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Title { get; set; }
}

/// <summary>
/// 控制点
/// </summary>
public class ControlPoint
{
    [JsonPropertyName("x")]
    public double X { get; set; }

    [JsonPropertyName("y")]
    public double Y { get; set; }
}