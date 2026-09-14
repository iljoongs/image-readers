using System.Windows;

namespace ImageTopicViewer.Views;

public partial class TopicProgressDialog : Window
{
    public int SelectedValue { get; private set; }

    public TopicProgressDialog(string topicName, int initialValue)
    {
        InitializeComponent();
        Title = "진행도 설정";
        MessageText.Text = $"'{topicName}'의 진행도(1~10)를 선택하세요.";

        SelectedValue = Math.Clamp(initialValue, 1, 10);
        ValueSlider.Value = SelectedValue;
        ValueText.Text = SelectedValue.ToString();
    }

    private void ValueSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        SelectedValue = (int)Math.Round(e.NewValue);
        if (ValueText is not null)
        {
            ValueText.Text = SelectedValue.ToString();
        }
    }

    private void OkButton_Click(object sender, RoutedEventArgs e) => DialogResult = true;
}
