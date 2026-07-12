using SipCoreMobile.Models;

namespace SipCoreMobile.Services;

/// <summary>
/// The original Kotlin WorkDesk files each had their own copy of essentially the same
/// find-contact/display-name/company-line helpers, prefixed differently per file
/// (workDeskFindContact/workDeskDisplayName in TaskDetailView, workDeskTaskListFindContact/
/// workDeskTaskListDisplayName in TaskListView, workDeskHomeDisplayName in HomeView, etc.) --
/// consolidated into one shared helper here rather than porting each file's copy separately.
/// </summary>
public static class WorkDeskContactHelpers
{
    public static ContactUi? FindContact(string extension, IReadOnlyList<ContactUi> contacts)
    {
        var clean = extension.Trim();
        if (string.IsNullOrEmpty(clean)) return null;
        return contacts.FirstOrDefault(c => string.Equals(c.Extension.Trim(), clean, StringComparison.OrdinalIgnoreCase));
    }

    public static string DisplayName(string extension, IReadOnlyList<ContactUi> contacts)
    {
        var clean = extension.Trim();
        if (string.IsNullOrEmpty(clean)) return "Unknown";

        var contact = FindContact(clean, contacts);
        if (contact is null) return clean;

        var cleanExt = contact.Extension.Trim();
        var cleanName = contact.DisplayName.Replace($"({cleanExt})", "").Trim();
        if (string.IsNullOrEmpty(cleanName)) cleanName = cleanExt;
        return $"{cleanName} ({cleanExt})";
    }

    public static string CompanyLine(string extension, IReadOnlyList<ContactUi> contacts)
    {
        var contact = FindContact(extension, contacts);
        if (contact is null) return "";

        if (contact.IsExternal && !string.IsNullOrWhiteSpace(contact.CompanyName)) return $"{contact.CompanyName} • External";
        return contact.CompanyName ?? "";
    }
}
