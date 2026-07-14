namespace SipCoreMobile.Controls;

public enum SnackKind { Info, Success, Error, Call, Message, Loading }

public partial class AppSnackbar : ContentView
{
    private CancellationTokenSource? _dismissCts;
    private Action? _tapAction;

    public AppSnackbar()
    {
        InitializeComponent();
    }

    public void Show(string title, string message, SnackKind kind = SnackKind.Info,
                     Action? onTap = null, int durationMs = 3500)
    {
        _dismissCts?.Cancel();
        _dismissCts = new CancellationTokenSource();
        var token = _dismissCts.Token;
        _tapAction = onTap;

        TitleLabel.Text = title;
        MessageLabel.Text = message;
        MessageLabel.IsVisible = !string.IsNullOrEmpty(message);
        ApplyKind(kind);

        IsVisible = true;
        Card.Opacity = 0;
        Card.TranslationY = -12;
        _ = Card.FadeTo(1, 180, Easing.CubicOut);
        _ = Card.TranslateTo(0, 0, 180, Easing.CubicOut);

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(durationMs, token);
                MainThread.BeginInvokeOnMainThread(async () => await HideAsync());
            }
            catch (OperationCanceledException) { /* replaced or dismissed */ }
        }, token);
    }

    private void ApplyKind(SnackKind kind)
    {
        (string glyph, string tileHex, string glyphHex) = kind switch
        {
            SnackKind.Success => ("✓", "#E7F8F1", "#0F8A58"),
            SnackKind.Error => ("!", "#FEECEB", "#B42318"),
            SnackKind.Call => ("📞", "#E7F8F1", "#0F8A58"),
            SnackKind.Message => ("✉", "#EAF1FF", "#0E42B3"),
            SnackKind.Loading => ("⏳", "#EEF1F6", "#40567A"),
            _ => ("ℹ", "#EAF1FF", "#0E42B3"),
        };
        IconLabel.Text = glyph;
        IconTile.BackgroundColor = Color.FromArgb(tileHex);
        IconLabel.TextColor = Color.FromArgb(glyphHex);
    }

    private async Task HideAsync()
    {
        await Card.FadeTo(0, 150, Easing.CubicIn);
        IsVisible = false;
    }

    private async void OnDismissTapped(object? sender, EventArgs e)
    {
        _dismissCts?.Cancel();
        await HideAsync();
    }

    private async void OnTapped(object? sender, EventArgs e)
    {
        _tapAction?.Invoke();
        _dismissCts?.Cancel();
        await HideAsync();
    }
}

/// <summary>Call InAppNotifier.Show(...) from anywhere; safe from any thread.</summary>
public static class InAppNotifier
{
    public static AppSnackbar? Host { get; set; }

    public static void Show(string title, string message = "", SnackKind kind = SnackKind.Info,
                            Action? onTap = null, int durationMs = 3500) =>
        MainThread.BeginInvokeOnMainThread(() => Host?.Show(title, message, kind, onTap, durationMs));
}