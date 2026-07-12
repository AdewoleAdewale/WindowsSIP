namespace SipCoreMobile.Controls;

public partial class StatusPulseDot : ContentView
{
    public static readonly BindableProperty IsLiveProperty = BindableProperty.Create(
        nameof(IsLive), typeof(bool), typeof(StatusPulseDot), false, propertyChanged: OnIsLiveChanged);

    public bool IsLive
    {
        get => (bool)GetValue(IsLiveProperty);
        set => SetValue(IsLiveProperty, value);
    }

    public StatusPulseDot()
    {
        InitializeComponent();
        Refresh();
    }

    private static void OnIsLiveChanged(BindableObject bindable, object oldValue, object newValue) =>
        ((StatusPulseDot)bindable).Refresh();

    private void Refresh()
    {
        var inactiveColor = Color.FromArgb("#98A2B3");

        if (IsLive)
        {
            GlowRing.Fill = new SolidColorBrush((Color)Application.Current!.Resources["SIPCoreSignalDim"]);
            CoreDot.Fill = new SolidColorBrush((Color)Application.Current!.Resources["SIPCoreSignal"]);
            StartPulse();
        }
        else
        {
            this.AbortAnimation("pulse");
            GlowRing.Opacity = 0;
            GlowRing.Scale = 1;
            CoreDot.Fill = new SolidColorBrush(inactiveColor);
        }
    }

    /// <summary>Gentle breathing glow -- restrained (per the design brief: one motif, quiet
    /// execution) rather than a flashy blink.</summary>
    private void StartPulse()
    {
        this.AbortAnimation("pulse");
        GlowRing.Opacity = 0.55;

        var animation = new Animation();
        animation.Add(0, 0.5, new Animation(v => GlowRing.Scale = v, 1.0, 1.8, Easing.SinInOut));
        animation.Add(0, 0.5, new Animation(v => GlowRing.Opacity = v, 0.55, 0.0, Easing.SinInOut));
        animation.Add(0.5, 1.0, new Animation(v => GlowRing.Scale = v, 1.0, 1.0));
        animation.Add(0.5, 1.0, new Animation(v => GlowRing.Opacity = v, 0.0, 0.0));

        animation.Commit(this, "pulse", length: 1800, repeat: () => IsLive);
    }
}
