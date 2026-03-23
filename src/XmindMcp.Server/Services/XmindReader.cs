using System.IO.Compression;
using System.Text.Json;
using XmindMcp.Server.Models;
// ReSharper disable ClassNeverInstantiated.Global

namespace XmindMcp.Server.Services;

/// <summary>
/// XMind 文件读取器
/// </summary>
public class XmindReader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = false
    };

    /// <summary>
    /// 加载 XMind 文件
    /// </summary>
    /// <param name="filePath">文件路径</param>
    /// <returns>XMind 文档</returns>
    public static XmindDocument Load(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"XMind file not found: {filePath}");
        }
        if (!filePath.EndsWith(".xmind", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("File must have .xmind extension");
        }
        using var archive = ZipFile.OpenRead(filePath);

        // 尝试读取现代格式 (content.json)
        var contentEntry = archive.GetEntry("content.json");
        if (contentEntry != null)
        {
            return LoadModernFormat(contentEntry, filePath);
        }

        // 尝试读取旧版格式 (content.xml)
        var xmlEntry = archive.GetEntry("content.xml");
        if (xmlEntry != null)
        {
            throw new NotSupportedException("XML format (XMind 8 and older) is not supported. Please use a modern XMind version.");
        }
        throw new InvalidOperationException("Invalid XMind file: no content found");
    }

    /// <summary>
    /// 加载现代 JSON 格式
    /// </summary>
    private static XmindDocument LoadModernFormat(ZipArchiveEntry entry, string filePath)
    {
        using var stream = entry.Open();
        using var reader = new StreamReader(stream);
        var json = reader.ReadToEnd();
        var sheets = JsonSerializer.Deserialize<List<JsonElement>>(json, JsonOptions) ?? throw new InvalidOperationException("Failed to parse XMind content");
        var doc = new XmindDocument { FilePath = filePath };
        foreach (var sheetJson in sheets)
        {
            doc.Sheets.Add(ParseSheet(sheetJson));
        }
        return doc;
    }

    /// <summary>
    /// 解析工作表
    /// </summary>
    private static Sheet ParseSheet(JsonElement json)
    {
        var sheet = new Sheet
        {
            Id = GetStringProperty(json, "id") ?? Guid.NewGuid().ToString(),
            Title = GetStringProperty(json, "sheetTitle") ?? "Sheet 1"
        };
        if (json.TryGetProperty("rootTopic", out var rootTopicJson))
        {
            sheet.RootTopic = ParseTopic(rootTopicJson);
        }
        if (json.TryGetProperty("relationships", out var relationshipsJson))
        {
            sheet.Relationships = ParseRelationships(relationshipsJson);
        }
        if (json.TryGetProperty("theme", out var themeJson))
        {
            sheet.Theme = new()
            {
                Id = GetStringProperty(themeJson, "id") ?? Guid.NewGuid().ToString(),
                Title = GetStringProperty(themeJson, "title") ?? "robust"
            };
        }
        return sheet;
    }

    /// <summary>
    /// 解析主题
    /// </summary>
    private static Topic ParseTopic(JsonElement json)
    {
        var topic = new Topic
        {
            Id = GetStringProperty(json, "id") ?? Guid.NewGuid().ToString(),
            Title = GetStringProperty(json, "title") ?? string.Empty,
            Href = GetStringProperty(json, "href")
        };

        // 解析备注
        if (json.TryGetProperty("notes", out var notesJson))
        {
            topic.Notes = ParseNotes(notesJson);
        }

        // 解析标记
        if (json.TryGetProperty("markers", out var markersJson))
        {
            topic.Markers = ParseMarkers(markersJson);
        }

        // 解析标签
        if (json.TryGetProperty("labels", out var labelsJson))
        {
            topic.Labels = ParseLabels(labelsJson);
        }

        // 递归解析子节点
        if (json.TryGetProperty("children", out var childrenJson))
        {
            if (childrenJson.TryGetProperty("attached", out var attachedJson))
            {
                var children = new List<Topic>();
                foreach (var child in attachedJson.EnumerateArray())
                {
                    var childTopic = ParseTopic(child);
                    childTopic.Parent = topic;
                    children.Add(childTopic);
                }
                if (children.Count > 0)
                {
                    topic.Children = new() { Attached = children };
                }
            }
        }
        return topic;
    }

    /// <summary>
    /// 解析备注
    /// </summary>
    private static TopicNotes? ParseNotes(JsonElement json)
    {
        if (json.TryGetProperty("plain", out var plainJson))
        {
            if (plainJson.TryGetProperty("content", out var contentJson))
            {
                return new()
                {
                    Plain = new()
                    {
                        Content = contentJson.GetString() ?? string.Empty
                    }
                };
            }
        }
        return null;
    }

    /// <summary>
    /// 解析标记
    /// </summary>
    private static List<Marker>? ParseMarkers(JsonElement json)
    {
        var markers = new List<Marker>();
        foreach (var marker in json.EnumerateArray())
        {
            markers.Add(new()
            {
                GroupId = GetStringProperty(marker, "groupId") ?? string.Empty,
                MarkerId = GetStringProperty(marker, "markerId") ?? string.Empty
            });
        }
        return markers.Count > 0 ? markers : null;
    }

    /// <summary>
    /// 解析标签
    /// </summary>
    private static List<string>? ParseLabels(JsonElement json)
    {
        var labels = new List<string>();
        foreach (var label in json.EnumerateArray())
        {
            if (label.GetString() is { } labelText)
            {
                labels.Add(labelText);
            }
        }
        return labels.Count > 0 ? labels : null;
    }

    /// <summary>
    /// 解析关系
    /// </summary>
    private static List<Relationship>? ParseRelationships(JsonElement json)
    {
        var relationships = new List<Relationship>();
        foreach (var rel in json.EnumerateArray())
        {
            relationships.Add(new()
            {
                Id = GetStringProperty(rel, "id") ?? Guid.NewGuid().ToString(),
                End1Id = GetStringProperty(rel, "end1Id") ?? string.Empty,
                End2Id = GetStringProperty(rel, "end2Id") ?? string.Empty,
                Title = GetStringProperty(rel, "title")
            });
        }
        return relationships.Count > 0 ? relationships : null;
    }

    /// <summary>
    /// 安全获取字符串属性
    /// </summary>
    private static string? GetStringProperty(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.String
            ? prop.GetString()
            : null;
}