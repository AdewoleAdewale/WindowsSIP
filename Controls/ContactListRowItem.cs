using SipCoreMobile.Models;

namespace SipCoreMobile.Controls;

/// <summary>Row model for ContactsPage's grouped CollectionView (mirrors CallHistoryRowItem's approach).</summary>
public class ContactListRowItem
{
    public bool IsHeader { get; init; }
    public string HeaderText { get; init; } = "";
    public ContactUi? Contact { get; init; }

    // Precomputed display fields (port of contactDisplayName/contactCleanName/presence logic)
    public string DisplayName { get; init; } = "";
    public string Initial { get; init; } = "";
    public bool IsOnline { get; init; }
    public string StatusText { get; init; } = "";
    public Color StatusColor { get; init; } = Colors.Gray;
    public string CompanySubtitle { get; init; } = "";
    public bool ShowCompanySubtitle { get; init; }
}

public class ContactRowTemplateSelector : DataTemplateSelector
{
    public DataTemplate? HeaderTemplate { get; set; }
    public DataTemplate? ContactTemplate { get; set; }

    protected override DataTemplate OnSelectTemplate(object item, BindableObject container) =>
        item is ContactListRowItem { IsHeader: true } ? HeaderTemplate! : ContactTemplate!;
}
