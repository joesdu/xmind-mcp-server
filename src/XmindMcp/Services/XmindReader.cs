using System.IO.Compression;
using System.Text.Json;
using XmindMcp.Models;

// ReSharper disable ClassNeverInstantiated.Global

namespace XmindMcp.Services;

/// <summary>
/// XMind 文件读取器
/// </summary>
public class XmindReader
{
    /// <summary>
    /// 异步加载 XMind 文件
    /// </summary>
    public static async Task<XmindDocument> LoadAsync(string filePath, CancellationToken cancellationToken = default)
    {
        ValidateFilePath(filePath);
        await using var archive = await ZipFile.OpenReadAsync(filePath, cancellationToken);
        var contentEntry = archive.GetEntry("content.json");
        if (contentEntry != null)
        {
            return await LoadModernFormatAsync(contentEntry, filePath, cancellationToken);
        }
        ThrowIfLegacyOrInvalidArchive(archive);
        return null!;
    }

    /// <summary>
    /// 异步加载现代 JSON 格式
    /// </summary>
    private static async Task<XmindDocument> LoadModernFormatAsync(ZipArchiveEntry entry, string filePath, CancellationToken cancellationToken)
    {
        await using var stream = await entry.OpenAsync(cancellationToken);
        var sheets = await JsonSerializer.DeserializeAsync<List<JsonElement>>(stream, XmindJson.ArchiveReadOptions, cancellationToken) ?? throw new InvalidOperationException("Failed to parse XMind content");
        return CreateDocument(sheets, filePath);
    }

    private static XmindDocument CreateDocument(List<JsonElement> sheets, string filePath)
    {
        var doc = new XmindDocument { FilePath = filePath };
        foreach (var sheetJson in sheets)
        {
            doc.Sheets.Add(ParseSheet(sheetJson));
        }
        return doc;
    }

    private static void ValidateFilePath(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"XMind file not found: {filePath}");
        }
        if (!filePath.EndsWith(".xmind", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("File must have .xmind extension");
        }
    }

    private static void ThrowIfLegacyOrInvalidArchive(ZipArchive archive)
    {
        var xmlEntry = archive.GetEntry("content.xml");
        if (xmlEntry != null)
        {
            throw new NotSupportedException("XML format (XMind 8 and older) is not supported. Please use a modern XMind version.");
        }
        throw new InvalidOperationException("Invalid XMind file: no content found");
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
        if (!json.TryGetProperty("children", out var childrenJson))
        {
            return topic;
        }
        if (!childrenJson.TryGetProperty("attached", out var attachedJson))
        {
            return topic;
        }
        var children = new List<Topic>();
        foreach (var childTopic in attachedJson.EnumerateArray().Select(ParseTopic))
        {
            childTopic.Parent = topic;
            children.Add(childTopic);
        }
        if (children.Count > 0)
        {
            topic.Children = new() { Attached = children };
        }
        return topic;
    }

    /// <summary>
    /// 解析备注
    /// </summary>
    private static TopicNotes? ParseNotes(JsonElement json)
    {
        if (!json.TryGetProperty("plain", out var plainJson))
        {
            return null;
        }
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
        return null;
    }

    /// <summary>
    /// 解析标记
    /// </summary>
    private static List<Marker>? ParseMarkers(JsonElement json)
    {
        var markers = json.EnumerateArray().Select(marker => new Marker
        {
            GroupId = GetStringProperty(marker, "groupId") ?? string.Empty,
            MarkerId = GetStringProperty(marker, "markerId") ?? string.Empty
        }).ToList();
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
        var relationships = json.EnumerateArray().Select(rel => new Relationship
        {
            Id = GetStringProperty(rel, "id") ?? Guid.NewGuid().ToString(),
            End1Id = GetStringProperty(rel, "end1Id") ?? string.Empty,
            End2Id = GetStringProperty(rel, "end2Id") ?? string.Empty,
            Title = GetStringProperty(rel, "title")
        }).ToList();
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