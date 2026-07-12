namespace SipCoreMobile.Services.Notifications;

/// <summary>
/// Original had this exact same cleanExtension() duplicated verbatim in both
/// NotificationHelper.kt and SipFirebaseMessagingService.kt; consolidated into one
/// shared helper here instead of duplicating it again.
/// </summary>
public static class PushTextUtils
{
    public static string CleanExtension(string value)
    {
        var raw = value.Trim();
        if (string.IsNullOrEmpty(raw)) return "";

        var openIdx = raw.IndexOf('(');
        var closeIdx = raw.IndexOf(')');
        var insideBracket = (openIdx >= 0 && closeIdx > openIdx)
            ? raw[(openIdx + 1)..closeIdx]
            : "";

        var selected = string.IsNullOrWhiteSpace(insideBracket) ? raw : insideBracket;

        var cleaned = selected
            .Replace("sip:", "", StringComparison.OrdinalIgnoreCase)
            .Replace("\"", "")
            .Replace("<", "")
            .Replace(">", "");

        var atIdx = cleaned.IndexOf('@');
        if (atIdx >= 0) cleaned = cleaned[..atIdx];

        return cleaned.Trim();
    }

    public static bool IsSystemMessage(string body)
    {
        var clean = body.Trim();
        return clean.StartsWith("SIPCORE_DELIVERED|", StringComparison.OrdinalIgnoreCase) ||
               clean.StartsWith("SIPCORE_READ|", StringComparison.OrdinalIgnoreCase) ||
               clean.StartsWith("SIPCORE_DELETE|", StringComparison.OrdinalIgnoreCase) ||
               clean.StartsWith("SIPCORE_REACTION|", StringComparison.OrdinalIgnoreCase);
    }

    public static string CleanMessagePreview(string body)
    {
        var clean = body.Trim();
        if (string.IsNullOrEmpty(clean)) return "New message";

        if (clean.StartsWith("SIPCORE_MSG|", StringComparison.OrdinalIgnoreCase))
        {
            var parts = clean.Split('|', 3);
            if (parts.Length >= 3) clean = parts[2];
        }

        if (clean.StartsWith("SIPCORE_REPLY|", StringComparison.OrdinalIgnoreCase))
        {
            var parts = clean.Split('|', 4);
            if (parts.Length >= 4) clean = parts[3];
        }

        if (clean.StartsWith("SIPCORE_MEDIA|voice|", StringComparison.OrdinalIgnoreCase))
            return "Voice note";

        if (clean.StartsWith("SIPCORE_MEDIA|attachment|", StringComparison.OrdinalIgnoreCase))
            return "Attachment";

        return clean.Length > 120 ? clean[..120] + "..." : clean;
    }
}
