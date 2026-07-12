namespace SipCoreMobile.Models;

public record PriorityChipStyle(Color BackgroundColor, Color TextColor);

/// <summary>Port of SIPCoreDesign.kt's priorityChipStyle(). Colors match SIPCoreColors.xaml.</summary>
public static class PriorityChipStyleFactory
{
    public static PriorityChipStyle For(string priority)
    {
        return priority.Trim().ToLowerInvariant() switch
        {
            "low" => new PriorityChipStyle(Color.FromArgb("#E8F8F0"), Color.FromArgb("#138A4B")),
            "normal" => new PriorityChipStyle(Color.FromArgb("#EAF2FF"), Color.FromArgb("#0057B8")),
            "high" => new PriorityChipStyle(Color.FromArgb("#FFF3E0"), Color.FromArgb("#E67700")),
            "critical" or "urgent" => new PriorityChipStyle(Color.FromArgb("#FFEAEA"), Color.FromArgb("#D32F2F")),
            _ => new PriorityChipStyle(Color.FromArgb("#F0F4FA"), Color.FromArgb("#002E6D"))
        };
    }
}
