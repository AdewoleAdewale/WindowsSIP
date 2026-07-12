using SipCoreMobile.Models;

namespace SipCoreMobile.Controls;

public class CallHistoryItemTemplateSelector : DataTemplateSelector
{
    public DataTemplate? HeaderTemplate { get; set; }
    public DataTemplate? LogTemplate { get; set; }

    protected override DataTemplate OnSelectTemplate(object item, BindableObject container) =>
        item is CallHistoryRowItem { IsHeader: true } ? HeaderTemplate! : LogTemplate!;
}
