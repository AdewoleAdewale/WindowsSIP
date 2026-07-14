using CommunityToolkit.Maui.Alerts;
using SipCoreMobile.Models;
using SipCoreMobile.ViewModels;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace SipCoreMobile.Views;

public partial class MainShellPage : ContentPage
{
    private const double CompactWidth = 900;

    private readonly AppStateViewModel _appState;
    private readonly IServiceProvider _services;
    private readonly ObservableCollection<ContactUi> _filtered = new();
    private bool _showAllExtensions;
    private ContentPage? _hostedPage;      // page whose Content the detail pane is showing
    private string? _selectedExtension;
    public MainShellPage(AppStateViewModel appState, IServiceProvider services)
    {
        InitializeComponent();
        _appState = appState;
        _services = services;
        BindingContext = appState;
        Controls.InAppNotifier.Host = Snackbar;
        ExtensionList.ItemsSource = _filtered;
        _appState.Contacts.CollectionChanged += OnContactsChanged;

        SizeChanged += OnPageSizeChanged;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        RefreshList();
        ProfileInitial.Text = string.IsNullOrEmpty(_appState.Extension)
      ? "?" : _appState.Extension[^1..];

        if (_hostedPage is null)
            ShowDashboard();
    }



    // ------------------------------------------------------------------
    // Detail pane hosting
    // ------------------------------------------------------------------

    private async void ShowDashboard()
    {
        _selectedExtension = null;
        ExtensionList.SelectedItem = null;

        var page = _services.GetRequiredService<WorkDeskDashboardPage>();
        HostPage(page);
        await page.InitializeHostedAsync();
    }

    private async void ShowChatFor(string extension)
    {
        if (_selectedExtension == extension) return;
        _selectedExtension = extension;

        var chatVm = _services.GetRequiredService<ChatViewModel>();  // singleton — same instance ChatPage gets
        var chatPage = _services.GetRequiredService<ChatPage>();

        HostPage(chatPage);                          // host first so the UI is live,
        await chatVm.OpenConversationAsync(extension); // then load — messages stream into the visible list
        chatPage.RefreshHeaderPublic();              // update name/avatar/status for the new extension
    }

    private void HostPage(ContentPage page)
    {
        // Detach previous
        if (_hostedPage is not null)
        {
            _hostedPage.SendDisappearing();
            DetailHost.Content = null;
        }

        _hostedPage = page;
        var content = page.Content;
        page.Content = null;              // detach from the page so it can be re-parented
        DetailHost.Content = content;
        page.SendAppearing();             // fire its lifecycle so data loads
    }

    // ------------------------------------------------------------------
    // List interactions
    // ------------------------------------------------------------------

    private void OnExtensionSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is ContactUi contact && contact.CanMessage)
        {
            ShowChatFor(contact.Extension);
            ApplyResponsiveLayout();
        }
    }

    private async void OnQuickCallClicked(object? sender, EventArgs e)
    {
        if (sender is ImageButton { CommandParameter: ContactUi contact })
            await _appState.MakeOutgoingCallAsync(contact.Extension);
    }

    private void OnQuickMessageClicked(object? sender, EventArgs e)
    {
        if (sender is ImageButton { CommandParameter: ContactUi contact })
            ExtensionList.SelectedItem = contact;
    }

    private void OnSearchTextChanged(object? sender, TextChangedEventArgs e) => RefreshList();

    private void OnContactsChanged(object? sender, NotifyCollectionChangedEventArgs e) =>
        MainThread.BeginInvokeOnMainThread(RefreshList);
    private void OnShowAllTapped(object? sender, EventArgs e)
    {
        _showAllExtensions = !_showAllExtensions;
        RefreshList();
    }
    private void RefreshList()
    {
        var q = SearchBox.Text?.Trim() ?? "";
        var searching = !string.IsNullOrEmpty(q);

        var items = _appState.Contacts.Where(c =>
            !searching ||
            c.DisplayName.Contains(q, StringComparison.OrdinalIgnoreCase) ||
            c.Extension.Contains(q, StringComparison.OrdinalIgnoreCase));

        // Default view: online only. Searching or "show all" reveals everyone.
        var hiddenOffline = 0;
        if (!searching && !_showAllExtensions)
        {
            hiddenOffline = items.Count(c => !c.IsOnline);
            items = items.Where(c => c.IsOnline);
        }

        _filtered.Clear();
        foreach (var c in items.OrderByDescending(c => c.IsOnline).ThenBy(c => c.DisplayName))
            _filtered.Add(c);

        OnlineCountLabel.Text = $"{_appState.Contacts.Count(c => c.IsOnline)} online";

        ShowAllRow.IsVisible = !searching;
        ShowAllLabel.Text = _showAllExtensions
            ? "Show active only"
            : hiddenOffline > 0 ? $"Show all extensions ({hiddenOffline} offline)" : "Show all extensions";
    }

    // ------------------------------------------------------------------
    // Nav rail  (mirrors BottomNavBar/drawer targets)
    // ------------------------------------------------------------------


    private void OnNavChatsTapped(object? s, EventArgs e) => ShowDashboardIfNoChat();
    private void OnNavHistoryTapped(object? s, EventArgs e) => _appState.CurrentScreen = Models.Screen.CallHistory;
    private void OnNavWorkDeskTapped(object? s, EventArgs e) => ShowDashboard();
    private void OnNavSettingsTapped(object? s, EventArgs e) => _appState.CurrentScreen = Models.Screen.Settings;
    private void OnNavProfileTapped(object? s, EventArgs e) => _appState.CurrentScreen = Models.Screen.Profile;
    private async void OnNavDialerTapped(object? s, EventArgs e)
    {
        _selectedExtension = null;
        ExtensionList.SelectedItem = null;
        var page = _services.GetRequiredService<DesktopCallsPage>();
        HostPage(page);
        await page.InitializeHostedAsync();
    }
    private void ShowDashboardIfNoChat()
    {
        if (_selectedExtension is null) ShowDashboard();
    }

    // ------------------------------------------------------------------
    // Responsive behavior
    // ------------------------------------------------------------------

    private void OnPageSizeChanged(object? sender, EventArgs e) => ApplyResponsiveLayout();

    private void ApplyResponsiveLayout()
    {
        if (Width <= 0) return;
        var compact = Width < CompactWidth;

        if (compact && _selectedExtension is not null)
        {
            // Chat open on a narrow window: give it the space, hide the list
            ListPane.IsVisible = false;
            RootGrid.ColumnDefinitions[1].Width = new GridLength(0);
        }
        else
        {
            ListPane.IsVisible = true;
            RootGrid.ColumnDefinitions[1].Width = compact ? new GridLength(280) : new GridLength(360);
        }
    }

    private async void TapGestureRecognizer_Tapped(object sender, TappedEventArgs e)
    {
        if (sender is ImageButton { CommandParameter: ContactUi contact })
            await _appState.MakeOutgoingCallAsync(contact.Extension);
    }

    private void TapGestureRecognizer_Tapped_1(object sender, TappedEventArgs e)
    {
        if (sender is ImageButton { CommandParameter: ContactUi contact })
            ExtensionList.SelectedItem = contact;
    }
}