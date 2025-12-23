using Microsoft.Maui.Controls;

namespace HiatMeApp.Controls;

public partial class FloatingLabelEntry : Grid
{
    public static readonly BindableProperty TextProperty =
        BindableProperty.Create(nameof(Text), typeof(string), typeof(FloatingLabelEntry), default(string), BindingMode.TwoWay);

    public static readonly BindableProperty LabelTextProperty =
        BindableProperty.Create(nameof(LabelText), typeof(string), typeof(FloatingLabelEntry), default(string));

    public static readonly BindableProperty KeyboardProperty =
        BindableProperty.Create(nameof(Keyboard), typeof(Keyboard), typeof(FloatingLabelEntry), Keyboard.Default);

    public static readonly BindableProperty IsPasswordProperty =
        BindableProperty.Create(nameof(IsPassword), typeof(bool), typeof(FloatingLabelEntry), false);

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public string LabelText
    {
        get => (string)GetValue(LabelTextProperty);
        set => SetValue(LabelTextProperty, value);
    }

    public Keyboard Keyboard
    {
        get => (Keyboard)GetValue(KeyboardProperty);
        set => SetValue(KeyboardProperty, value);
    }

    public bool IsPassword
    {
        get => (bool)GetValue(IsPasswordProperty);
        set => SetValue(IsPasswordProperty, value);
    }

    public FloatingLabelEntry()
    {
        InitializeComponent();
        
        // Bind Entry properties
        EntryField.SetBinding(Entry.TextProperty, new Binding(nameof(Text), source: this, mode: BindingMode.TwoWay));
        EntryField.SetBinding(Entry.KeyboardProperty, new Binding(nameof(Keyboard), source: this));
        EntryField.SetBinding(Entry.IsPasswordProperty, new Binding(nameof(IsPassword), source: this));
        
        // Bind Label text
        FloatingLabel.SetBinding(Label.TextProperty, new Binding(nameof(LabelText), source: this));
    }
}

