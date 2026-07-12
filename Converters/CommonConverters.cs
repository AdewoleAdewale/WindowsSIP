using System.Globalization;

namespace SipCoreMobile.Converters;

public class InverseBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        !(value is bool b && b);

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        !(value is bool b && b);
}

/// <summary>Port of LoginScreen's `if (isLoggingIn) "Registering..." else "Login"`.</summary>
public class LoginButtonTextConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is true ? "Registering..." : "Login";

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>Port of formatDuration() from SIPCoreApp.kt.</summary>
public class DurationFormatConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var seconds = value is int i ? i : 0;
        return $"{seconds / 60:D2}:{seconds % 60:D2}";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>
/// Port of the "Name (ext)" splitting done inline in IncomingCallScreen/ActiveCallScreen
/// (`text.substringBefore("(").trim()`). Extracts the name part.
/// </summary>
public class DisplayNamePartConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var text = value as string ?? "";
        var idx = text.IndexOf('(');
        var name = (idx >= 0 ? text[..idx] : text).Trim();
        return string.IsNullOrEmpty(name) ? "Unknown Caller" : name;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>Extracts the "(ext)" part, e.g. "Jane Doe (1001)" → "1001".</summary>
public class ExtensionPartConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var text = value as string ?? "";
        var openIdx = text.IndexOf('(');
        var closeIdx = text.IndexOf(')');
        return (openIdx >= 0 && closeIdx > openIdx) ? text[(openIdx + 1)..closeIdx].Trim() : "";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>Port of `contact.displayName.take(1).uppercase()` for the avatar initial.</summary>
public class FirstLetterUpperConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var text = value as string ?? "";
        return text.Length > 0 ? text[..1].ToUpperInvariant() : "";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>Port of formatMessageTime(): unix-ms → "HH:mm".</summary>
public class MessageTimeConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not long raw || raw <= 0) return "";
        var normalized = raw < 10_000_000_000L ? raw * 1000 : raw;
        return DateTimeOffset.FromUnixTimeMilliseconds(normalized).ToLocalTime().ToString("HH:mm");
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>Port of statusTicks(): Sending→⏱, Sent→✓, Delivered/Read→✓✓, Failed→!.</summary>
public class StatusTicksConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is Models.MessageStatus status
            ? status switch
            {
                Models.MessageStatus.Sending => "⏱",
                Models.MessageStatus.Sent => "✓",
                Models.MessageStatus.Delivered => "✓✓",
                Models.MessageStatus.Read => "✓✓",
                Models.MessageStatus.Failed => "!",
                _ => ""
            }
            : "";

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>Port of `extension.takeLast(2)` for the conversation-list avatar.</summary>
public class LastTwoCharsConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var text = value as string ?? "";
        return text.Length <= 2 ? text : text[^2..];
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>int > 0 → bool, for unread-count badges (`if (convo.unreadCount > 0)`).</summary>
public class IntGreaterThanZeroConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is int i && i > 0;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>String.IsNullOrEmpty → bool, for Extension text visibility (`if (extension.isNotBlank())`).</summary>
public class StringNotEmptyConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        !string.IsNullOrWhiteSpace(value as string);

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
