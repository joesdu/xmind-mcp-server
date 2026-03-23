using System.Text.Json.Serialization;

// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global

namespace XmindMcp.Server.Models;

/// <summary>
/// XMind 文档
/// </summary>
public class XmindDocument
{
    /// <summary>
    /// 文件路径
    /// </summary>
    [JsonIgnore]
    public string? FilePath { get; set; }

    /// <summary>
    /// 工作表列表
    /// </summary>
    public List<Sheet> Sheets { get; set; } = [];

    /// <summary>
    /// 获取第一个工作表
    /// </summary>
    public Sheet? GetActiveSheet() => Sheets.FirstOrDefault();

    /// <summary>
    /// 根据标题查找工作表
    /// </summary>
    public Sheet? FindSheet(string title) => Sheets.FirstOrDefault(s => s.Title.Equals(title, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// 删除工作表
    /// </summary>
    public bool RemoveSheet(string title)
    {
        var sheet = FindSheet(title);
        return sheet != null && Sheets.Remove(sheet);
    }

    /// <summary>
    /// 重命名工作表
    /// </summary>
    public bool RenameSheet(string currentTitle, string newTitle)
    {
        var sheet = FindSheet(currentTitle);
        if (sheet == null)
        {
            return false;
        }
        sheet.Title = newTitle;
        return true;
    }

    /// <summary>
    /// 添加工作表
    /// </summary>
    public Sheet AddSheet(string title, string rootTopicTitle)
    {
        var sheet = new Sheet
        {
            Title = title,
            RootTopic = new() { Title = rootTopicTitle }
        };
        Sheets.Add(sheet);
        return sheet;
    }
}