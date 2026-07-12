namespace SipCoreMobile.Models;

public record WorkDeskChipStyle(Color BackgroundColor, Color TextColor);

/// <summary>Port of WorkDeskUiCommon.kt's workDeskStatusStyle()/workDeskPriorityStyle().</summary>
public static class WorkDeskChipStyleFactory
{
    public static WorkDeskChipStyle ForStatus(string status) => status.Trim().ToLowerInvariant() switch
    {
        "pending" => new WorkDeskChipStyle(Color.FromArgb("#FFF3E0"), Color.FromArgb("#E67700")),
        "in progress" => new WorkDeskChipStyle(Color.FromArgb("#EAF2FF"), Color.FromArgb("#0057B8")),
        "completed" => new WorkDeskChipStyle(Color.FromArgb("#E8F5E9"), Color.FromArgb("#2E7D32")),
        "overdue" => new WorkDeskChipStyle(Color.FromArgb("#FFEBEE"), Color.FromArgb("#D32F2F")),
        "pending approval" or "pendingapproval" => new WorkDeskChipStyle(Color.FromArgb("#F3E5F5"), Color.FromArgb("#7B1FA2")),
        "on hold" => new WorkDeskChipStyle(Color.FromArgb("#F0F4FA"), Color.FromArgb("#002E6D")),
        _ => new WorkDeskChipStyle(Color.FromArgb("#EAF2FF"), Color.FromArgb("#0057B8"))
    };

    public static WorkDeskChipStyle ForPriority(string priority) => priority.Trim().ToLowerInvariant() switch
    {
        "low" => new WorkDeskChipStyle(Color.FromArgb("#E8F8F0"), Color.FromArgb("#138A4B")),
        "normal" => new WorkDeskChipStyle(Color.FromArgb("#EAF2FF"), Color.FromArgb("#0057B8")),
        "high" => new WorkDeskChipStyle(Color.FromArgb("#FFF3E0"), Color.FromArgb("#E67700")),
        "critical" or "urgent" => new WorkDeskChipStyle(Color.FromArgb("#FFEAEA"), Color.FromArgb("#D32F2F")),
        _ => new WorkDeskChipStyle(Color.FromArgb("#F0F4FA"), Color.FromArgb("#002E6D"))
    };
}
