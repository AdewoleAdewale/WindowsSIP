using System.Collections.Specialized;
using SipCoreMobile.Models;
using SipCoreMobile.ViewModels;

namespace SipCoreMobile.Views;

public partial class MeetingCreatePage : ContentPage
{
    private readonly AppStateViewModel _appState;
    private readonly MeetingsViewModel _viewModel;

    private bool _isInstant = true;
    private bool _isRecorded;
    private readonly HashSet<string> _selectedParticipants = new();

    public MeetingCreatePage(AppStateViewModel appState, MeetingsViewModel viewModel)
    {
        InitializeComponent();
        _appState = appState;
        _viewModel = viewModel;
        BindingContext = viewModel;

        InstantOption.IsChecked = true;
        _appState.Contacts.CollectionChanged += OnContactsChanged;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        ApplyFilter(ParticipantSearchBox.Text ?? "");
    }

    private void OnContactsChanged(object? sender, NotifyCollectionChangedEventArgs e) => ApplyFilter(ParticipantSearchBox.Text ?? "");
    private void OnParticipantSearchChanged(object? sender, TextChangedEventArgs e) => ApplyFilter(e.NewTextValue ?? "");

    /// <summary>Port of MeetingCreateView's filteredContacts derivation.</summary>
    private void ApplyFilter(string search)
    {
        var q = search.Trim().ToLowerInvariant();

        var filtered = _appState.Contacts
            .Where(c => c.Extension != _appState.Extension)
            .Where(c => string.IsNullOrEmpty(q)
                     || c.DisplayName.ToLowerInvariant().Contains(q)
                     || c.Extension.ToLowerInvariant().Contains(q))
            .ToList();

        EmptyParticipants.IsVisible = filtered.Count == 0;
        ParticipantsList.IsVisible = filtered.Count > 0;
        ParticipantsList.ItemsSource = filtered;
    }

    private void OnInstantChanged(object? sender, bool value) => _isInstant = value;
    private void OnRecordedChanged(object? sender, bool value) => _isRecorded = value;

    private void OnParticipantCheckedChanged(object? sender, CheckedChangedEventArgs e)
    {
        if (sender is not CheckBox { BindingContext: ContactUi contact }) return;

        if (e.Value) _selectedParticipants.Add(contact.Extension);
        else _selectedParticipants.Remove(contact.Extension);

        SelectedCountLabel.Text = $"{_selectedParticipants.Count} selected";
    }

    private async void OnCancelClicked(object? sender, EventArgs e) => await Navigation.PopAsync();

    private async void OnCreateClicked(object? sender, EventArgs e)
    {
        var title = string.IsNullOrWhiteSpace(TitleEntry.Text) ? "SIPCore Meeting" : TitleEntry.Text.Trim();
        var description = DescriptionEditor.Text ?? "";

        await _viewModel.CreateMeetingAsync(title, description, _selectedParticipants.ToList(), _isInstant, _isRecorded);
        await Navigation.PopAsync();
    }
}
