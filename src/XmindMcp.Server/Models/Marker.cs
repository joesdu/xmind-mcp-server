using System.Text.Json.Serialization;
// ReSharper disable UnusedMember.Global

namespace XmindMcp.Server.Models;

/// <summary>
/// XMind 标记
/// </summary>
public class Marker
{
    /// <summary>
    /// 标记组 ID
    /// </summary>
    [JsonPropertyName("groupId")]
    public string GroupId { get; set; } = string.Empty;

    /// <summary>
    /// 标记 ID
    /// </summary>
    [JsonPropertyName("markerId")]
    public string MarkerId { get; set; } = string.Empty;
}

/// <summary>
/// 常用标记常量
/// </summary>
public static class MarkerConstants
{
    public const string FlagBlue = "flag-blue";
    public const string FlagGreen = "flag-green";
    public const string FlagOrange = "flag-orange";

    // 旗帜标记
    public const string FlagRed = "flag-red";

    // 优先级标记
    public const string Priority1 = "priority-1";
    public const string Priority2 = "priority-2";
    public const string Priority3 = "priority-3";
    public const string SmileySad = "smiley-sad";

    // 表情标记
    public const string SmileySmile = "smiley-smile";
    public const string SmileySurprise = "smiley-surprise";
    public const string SymbolMinus = "symbol-minus";

    // 符号标记
    public const string SymbolPlus = "symbol-plus";
    public const string SymbolQuestion = "symbol-question";
    public const string TaskDoing = "task-Doing";

    // 任务状态标记
    public const string TaskDone = "task-done";
    public const string TaskTodo = "task-oct";

    /// <summary>
    /// 创建优先级标记
    /// </summary>
    public static Marker Priority(int level) =>
        new()
        {
            GroupId = "priorityMarkers",
            MarkerId = $"priority-{level}"
        };

    /// <summary>
    /// 创建任务状态标记
    /// </summary>
    public static Marker Task(string status) =>
        new()
        {
            GroupId = "taskMarkers",
            MarkerId = $"task-{status}"
        };

    /// <summary>
    /// 创建旗帜标记
    /// </summary>
    public static Marker Flag(string color) =>
        new()
        {
            GroupId = "flagMarkers",
            MarkerId = $"flag-{color}"
        };
}