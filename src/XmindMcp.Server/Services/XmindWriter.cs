using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using XmindMcp.Server.Models;
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedMember.Global

namespace XmindMcp.Server.Services;

/// <summary>
/// XMind 文件写入器
/// </summary>
public class XmindWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>
    /// 保存 XMind 文件
    /// </summary>
    /// <param name="document">XMind 文档</param>
    /// <param name="filePath">文件路径</param>
    public static void Save(XmindDocument document, string? filePath = null)
    {
        var targetPath = filePath ?? document.FilePath ?? throw new ArgumentException("No file path specified");

        // 确保目录存在
        var directory = Path.GetDirectoryName(targetPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        // 删除已存在的文件
        if (File.Exists(targetPath))
        {
            File.Delete(targetPath);
        }

        // 直接创建 ZIP 文件到目标路径
        using (var archive = ZipFile.Open(targetPath, ZipArchiveMode.Create))
        {
            // 写入 content.json
            WriteContentJson(archive, document.Sheets);

            // 写入 manifest.json
            WriteManifestJson(archive);

            // 写入 metadata.json
            WriteMetadataJson(archive, document.Sheets.FirstOrDefault()?.Id);

            // 写入 metadata/content.json
            WriteMetadataContentJson(archive);
        }

        document.FilePath = targetPath;
    }

    /// <summary>
    /// 写入 content.json
    /// </summary>
    private static void WriteContentJson(ZipArchive archive, List<Sheet> sheets)
    {
        var entry = archive.CreateEntry("content.json", CompressionLevel.Optimal);
        using var stream = entry.Open();
        var sheetsJson = sheets.Select(SerializeSheet).ToList();
        var json = JsonSerializer.Serialize(sheetsJson, JsonOptions);
        var bytes = Encoding.UTF8.GetBytes(json);
        stream.Write(bytes, 0, bytes.Length);
    }

    /// <summary>
    /// 序列化工作表
    /// </summary>
    private static object SerializeSheet(Sheet sheet)
    {
        var result = new Dictionary<string, object>
        {
            ["id"] = sheet.Id,
            ["sheetTitle"] = sheet.Title,
            ["rootTopic"] = SerializeTopic(sheet.RootTopic)
        };
        if (sheet.Relationships is { Count: > 0 })
        {
            result["relationships"] = sheet.Relationships.Select(r => new
            {
                id = r.Id,
                end1Id = r.End1Id,
                end2Id = r.End2Id,
                title = r.Title,
                controlPoints = r.ControlPoints
            }).ToList();
        }
        if (sheet.Theme != null)
        {
            result["theme"] = new
            {
                id = sheet.Theme.Id,
                title = sheet.Theme.Title
            };
        }
        return result;
    }

    /// <summary>
    /// 序列化主题
    /// </summary>
    private static object SerializeTopic(Topic topic)
    {
        var result = new Dictionary<string, object>
        {
            ["id"] = topic.Id,
            ["title"] = topic.Title
        };
        if (topic.Notes != null)
        {
            result["notes"] = new
            {
                plain = new { content = topic.Notes.Plain?.Content ?? string.Empty }
            };
        }
        if (topic.Markers is { Count: > 0 })
        {
            result["markers"] = topic.Markers.Select(m => new
            {
                groupId = m.GroupId,
                markerId = m.MarkerId
            }).ToList();
        }
        if (topic.Labels is { Count: > 0 })
        {
            result["labels"] = topic.Labels;
        }
        if (topic.Href != null)
        {
            result["href"] = topic.Href;
        }
        if (topic.Children?.Attached is { Count: > 0 })
        {
            result["children"] = new
            {
                attached = topic.Children.Attached.Select(SerializeTopic).ToList()
            };
        }
        return result;
    }

    /// <summary>
    /// 写入 manifest.json
    /// </summary>
    private static void WriteManifestJson(ZipArchive archive)
    {
        var entry = archive.CreateEntry("manifest.json", CompressionLevel.Optimal);
        using var stream = entry.Open();
        
        // XMind 标准 manifest 格式
        var manifest = new Dictionary<string, object>
        {
            ["file-entries"] = new Dictionary<string, object>
            {
                ["content.json"] = new { },
                ["metadata.json"] = new { }
            }
        };
        
        var json = JsonSerializer.Serialize(manifest, JsonOptions);
        var bytes = Encoding.UTF8.GetBytes(json);
        stream.Write(bytes, 0, bytes.Length);
    }

    /// <summary>
    /// 写入 metadata.json
    /// </summary>
    private static void WriteMetadataJson(ZipArchive archive, string? activeSheetId)
    {
        var entry = archive.CreateEntry("metadata.json", CompressionLevel.Optimal);
        using var stream = entry.Open();
        var metadata = new
        {
            creator = new
            {
                name = "XmindMcp",
                version = "1.0.0"
            },
            activeSheetId = activeSheetId ?? string.Empty
        };
        var json = JsonSerializer.Serialize(metadata, JsonOptions);
        var bytes = Encoding.UTF8.GetBytes(json);
        stream.Write(bytes, 0, bytes.Length);
    }

    /// <summary>
    /// 写入 metadata/content.json
    /// </summary>
    private static void WriteMetadataContentJson(ZipArchive archive)
    {
        var entry = archive.CreateEntry("metadata/content.json", CompressionLevel.Optimal);
        using var stream = entry.Open();

        // 空的 metadata content
        var bytes = "{}"u8.ToArray();
        stream.Write(bytes, 0, bytes.Length);
    }

    /// <summary>
    /// 异步保存 XMind 文件
    /// </summary>
    public static async Task SaveAsync(XmindDocument document, string? filePath = null, CancellationToken cancellationToken = default)
    {
        await Task.Run(() => Save(document, filePath), cancellationToken);
    }
}