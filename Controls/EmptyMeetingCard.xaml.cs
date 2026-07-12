namespace SipCoreMobile.Controls;

public partial class EmptyMeetingCard : ContentView
{
    public static readonly BindableProperty TitleProperty = BindableProperty.Create(
        nameof(Title), typeof(string), typeof(EmptyMeetingCard), "", propertyChanged: OnTitleChanged);

    public static readonly BindableProperty BodyProperty = BindableProperty.Create(
        nameof(Body), typeof(string), typeof(EmptyMeetingCard), "", propertyChanged: OnBodyChanged);

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string Body
    {
        get => (string)GetValue(BodyProperty);
        set => SetValue(BodyProperty, value);
    }

    public EmptyMeetingCard()
    {
        InitializeComponent();
    }

    private static void OnTitleChanged(BindableObject bindable, object oldValue, object newValue) =>
        ((EmptyMeetingCard)bindable).TitleLabel.Text = (string)newValue;

    private static void OnBodyChanged(BindableObject bindable, object oldValue, object newValue) =>
        ((EmptyMeetingCard)bindable).BodyLabel.Text = (string)newValue;
}
