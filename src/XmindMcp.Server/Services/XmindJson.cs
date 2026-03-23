using System.Text.Json;
using System.Text.Json.Serialization;

namespace XmindMcp.Server.Services;

internal static class XmindJson
{
    public static readonly JsonSerializerOptions ArchiveReadOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static readonly JsonSerializerOptions ArchiveWriteOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static readonly JsonSerializerOptions ToolResponseOptions = new()
    {
        WriteIndented = true
    };
}