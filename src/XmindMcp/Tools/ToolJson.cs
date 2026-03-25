using System.Text.Json;
using XmindMcp.Services;

namespace XmindMcp.Tools;

internal static class ToolJson
{
    public static string Serialize(object value) => JsonSerializer.Serialize(value, XmindJson.ToolResponseOptions);

    public static string Error(string message) => Serialize(new { error = message });
}