using System.Windows;
using System.Windows.Controls;

namespace ImageTopicViewer.Views;

public partial class TextInputDialog : Window
{
    private readonly Func<string, string?> _validate;

    public string InputText => InputTextBox.Text;

    public TextInputDialog(string title, string message, Func<string, string?> validate, string initialValue = "")
    {
        InitializeComponent();
        Title = title;
        MessageText.Text = message;
        _validate = validate;
        InputTextBox.Text = initialValue;
        InputTextBox.SelectAll();
        Validate();
    }

    private void InputTextBox_TextChanged(object sender, TextChangedEventArgs e) => Validate();

    private void Validate()
    {
        var error = _validate(InputTextBox.Text);
        ErrorText.Text = error ?? string.Empty;
        OkButton.IsEnabled = error is null;
    }

    private void OkButton_Click(object sender, RoutedEventArgs e) => DialogResult = true;
}
