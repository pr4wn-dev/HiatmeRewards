using Microsoft.Maui.Controls;
using Microsoft.Maui.ApplicationModel;
using System.Threading.Tasks;

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
        
        // Listen for property changes to update label state
        this.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(Text))
            {
                UpdateLabelState();
            }
        };
        
        // Set initial state - placeholder mode (large, centered)
        UpdateLabelState();
        
        // Listen for text changes to update label state
        EntryField.TextChanged += (s, e) => UpdateLabelState();
        EntryField.Focused += (s, e) => UpdateLabelState();
        EntryField.Unfocused += (s, e) => UpdateLabelState();
        
        // Also update after a short delay to handle initial binding
        Task.Delay(100).ContinueWith(_ =>
        {
            MainThread.BeginInvokeOnMainThread(() => UpdateLabelState());
        });
    }
    
    private void UpdateLabelState()
    {
        bool hasText = !string.IsNullOrEmpty(EntryField.Text);
        bool isFocused = EntryField.IsFocused;
        
        if (hasText || isFocused)
        {
            // Floating state (small, top)
            FloatingLabel.Margin = new Thickness(16, 2, 0, 0);
            FloatingLabel.FontSize = 12;
        }
        else
        {
            // Placeholder state (large, centered)
            FloatingLabel.Margin = new Thickness(16, 18, 0, 0);
            FloatingLabel.FontSize = 18;
        }
    }
}

