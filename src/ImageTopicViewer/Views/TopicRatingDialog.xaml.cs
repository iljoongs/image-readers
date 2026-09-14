using System.Windows;

namespace ImageTopicViewer.Views;

public partial class TopicRatingDialog : Window
{
    public int SelectedValue { get; private set; }

    public TopicRatingDialog(string topicName, int initialValue)
    {
        InitializeComponent();
        Title = "평점 설정";
        MessageText.Text = $"'{topicName}'의 평점(1~10)을 선택하세요.";

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
