using SipCoreMobile.Models;

namespace SipCoreMobile.Views;

public partial class WorkDeskContactPickerPage : ContentPage
{
    private readonly TaskCompletionSource<List<string>> _completion = new();
    private readonly List<ContactPickerRow> _rows;

    private WorkDeskContactPickerPage(string title, IReadOnlyList<ContactUi> contacts, IReadOnlyList<string> selectedExtensions)
    {
        InitializeComponent();
        TitleLabel.Text = title;

        _rows = contacts.Select(c => new ContactPickerRow
        {
            Contact = c,
            IsSelected = selectedExtensions.Any(e => string.Equals(e.Trim(), c.Extension.Trim(), StringComparison.OrdinalIgnoreCase))
        }).ToList();

        ContactsList.ItemsSource = _rows;
    }

    /// <summary>
    /// Port of WorkDeskContactPickerDialog. Original was a Compose AlertDialog with a
    /// scrollable list of toggleable rows and a single "Done" confirm button; ported as an
    /// async modal page instead (same pattern as Batch 5's AddConferenceParticipantPage),
    /// since it needs to return the final selection to the caller.
    /// </summary>
    public static async Task<List<string>> ShowAsync(
        INavigation navigation, string title, IReadOnlyList<ContactUi> contacts, IReadOnlyList<string> selectedExtensions)
    {
        var page = new WorkDeskContactPickerPage(title, contacts, selectedExtensions);
        await navigation.PushModalAsync(page);
        return await page._completion.Task;
    }

    private void OnContactTapped(object? sender, TappedEventArgs e)
    {
        if (sender is not Grid { BindingContext: ContactPickerRow row }) return;

        row.IsSelected = !row.IsSelected;

        // CollectionView doesn't observe property changes on plain objects; refresh the
        // ItemsSource to force the check mark to redraw.
        ContactsList.ItemsSource = null;
        ContactsList.ItemsSource = _rows;
    }

    private async void OnDoneClicked(object? sender, EventArgs e)
    {
        var selected = _rows.Where(r => r.IsSelected).Select(r => r.Contact.Extension).ToList();
        _completion.TrySetResult(selected);
        await Navigation.PopModalAsync();
    }
}
