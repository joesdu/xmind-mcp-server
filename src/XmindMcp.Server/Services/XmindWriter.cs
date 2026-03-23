using System.IO.Compression;
using System.Text.Json;
using XmindMcp.Server.Models;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedMember.Global

namespace XmindMcp.Server.Services;

/// <summary>
/// XMind 文件写入器
/// </summary>
public class XmindWriter
{
    /// <summary>
    /// 保存 XMind 文件
    /// </summary>
    /// <param name="document">XMind 文档</param>
    /// <param name="filePath">文件路径</param>
    public static void Save(XmindDocument document, string? filePath = null)
    {
        var targetPath = filePath ?? document.FilePath ?? throw new ArgumentException("No file path specified");
        PrepareTargetPath(targetPath);
        using var archive = ZipFile.Open(targetPath, ZipArchiveMode.Create);
        WriteContentJson(archive, document.Sheets);
        WriteManifestJson(archive);
        WriteMetadataJson(archive, document.Sheets.FirstOrDefault()?.Id);
        WriteMetadataContentJson(archive);
        document.FilePath = targetPath;
    }

    /// <summary>
    /// 异步保存 XMind 文件
    /// </summary>
    public static async Task SaveAsync(XmindDocument document, string? filePath = null, CancellationToken cancellationToken = default)
    {
        var targetPath = filePath ?? document.FilePath ?? throw new ArgumentException("No file path specified");
        PrepareTargetPath(targetPath);
        await using var archive = await ZipFile.OpenAsync(targetPath, ZipArchiveMode.Create, cancellationToken);
        await WriteContentJsonAsync(archive, document.Sheets, cancellationToken);
        await WriteManifestJsonAsync(archive, cancellationToken);
        await WriteMetadataJsonAsync(archive, document.Sheets.FirstOrDefault()?.Id, cancellationToken);
        await WriteMetadataContentJsonAsync(archive, cancellationToken);
        document.FilePath = targetPath;
    }

    /// <summary>
    /// 写入 content.json
    /// </summary>
    private static void WriteContentJson(ZipArchive archive, List<Sheet> sheets)
    {
        var sheetsJson = sheets.Select(SerializeSheet).ToList();
        WriteJsonEntry(archive, "content.json", sheetsJson);
    }

    private static Task WriteContentJsonAsync(ZipArchive archive, List<Sheet> sheets, CancellationToken cancellationToken)
    {
        var sheetsJson = sheets.Select(SerializeSheet).ToList();
        return WriteJsonEntryAsync(archive, "content.json", sheetsJson, cancellationToken);
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
        var manifest = new Dictionary<string, object>
        {
            ["file-entries"] = new Dictionary<string, object>
            {
                ["content.json"] = new { },
                ["metadata.json"] = new { },
                ["metadata/content.json"] = new { }
            }
        };
        WriteJsonEntry(archive, "manifest.json", manifest);
    }

    private static Task WriteManifestJsonAsync(ZipArchive archive, CancellationToken cancellationToken)
    {
        var manifest = new Dictionary<string, object>
        {
            ["file-entries"] = new Dictionary<string, object>
            {
                ["content.json"] = new { },
                ["metadata.json"] = new { },
                ["metadata/content.json"] = new { }
            }
        };
        return WriteJsonEntryAsync(archive, "manifest.json", manifest, cancellationToken);
    }

    /// <summary>
    /// 写入 metadata.json
    /// </summary>
    private static void WriteMetadataJson(ZipArchive archive, string? activeSheetId)
    {
        var metadata = new
        {
            creator = new
            {
                name = "XmindMcp",
                version = "1.0.0"
            },
            activeSheetId = activeSheetId ?? string.Empty
        };
        WriteJsonEntry(archive, "metadata.json", metadata);
    }

    private static Task WriteMetadataJsonAsync(ZipArchive archive, string? activeSheetId, CancellationToken cancellationToken)
    {
        var metadata = new
        {
            creator = new
            {
                name = "XmindMcp",
                version = "1.0.0"
            },
            activeSheetId = activeSheetId ?? string.Empty
        };
        return WriteJsonEntryAsync(archive, "metadata.json", metadata, cancellationToken);
    }

    /// <summary>
    /// 写入 metadata/content.json
    /// </summary>
    private static void WriteMetadataContentJson(ZipArchive archive)
    {
        WriteJsonEntry(archive, "metadata/content.json", new { });
    }

    private static Task WriteMetadataContentJsonAsync(ZipArchive archive, CancellationToken cancellationToken) => WriteJsonEntryAsync(archive, "metadata/content.json", new { }, cancellationToken);

    private static void PrepareTargetPath(string targetPath)
    {
        var directory = Path.GetDirectoryName(targetPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }
        if (File.Exists(targetPath))
        {
            File.Delete(targetPath);
        }
    }

    private static void WriteJsonEntry(ZipArchive archive, string entryName, object payload)
    {
        var entry = archive.CreateEntry(entryName, CompressionLevel.Optimal);
        using var stream = entry.Open();
        JsonSerializer.Serialize(stream, payload, payload.GetType(), XmindJson.ArchiveWriteOptions);
    }

    private static async Task WriteJsonEntryAsync(ZipArchive archive, string entryName, object payload, CancellationToken cancellationToken)
    {
        var entry = archive.CreateEntry(entryName, CompressionLevel.Optimal);
        await using var stream = await entry.OpenAsync(cancellationToken);
        await JsonSerializer.SerializeAsync(stream, payload, payload.GetType(), XmindJson.ArchiveWriteOptions, cancellationToken);
    }
}