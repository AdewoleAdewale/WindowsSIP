using SipCoreMobile.Models;

namespace SipCoreMobile.Views;

public class ContactPickerRow
{
    public required ContactUi Contact { get; init; }
    public bool IsSelected { get; set; }
    public string DisplayLabel => $"{Contact.DisplayName}";
    public string Subtitle => $"Ext. {Contact.Extension}{(Contact.IsExternal ? $" • {Contact.CompanyName}" : "")}";
}
