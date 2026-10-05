using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
namespace MenuBarStats.Windows;
public partial class MeterControl : UserControl
{
    public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(nameof(Title), typeof(string), typeof(MeterControl), new PropertyMetadata(""));
    public static readonly DependencyProperty DetailProperty = DependencyProperty.Register(nameof(Detail), typeof(string), typeof(MeterControl), new PropertyMetadata(""));
    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(nameof(Value), typeof(double), typeof(MeterControl), new PropertyMetadata(0.0, Changed));
    public string Title { get => (string)GetValue(TitleProperty); set => SetValue(TitleProperty, value); }
    public string Detail { get => (string)GetValue(DetailProperty); set => SetValue(DetailProperty, value); }
    public double Value { get => (double)GetValue(ValueProperty); set => SetValue(ValueProperty, value); }
    public MeterControl() { InitializeComponent(); Loaded += (_, _) => UpdateTint(); }
    private static void Changed(DependencyObject d, DependencyPropertyChangedEventArgs e) => ((MeterControl)d).UpdateTint();
    private void UpdateTint()
    {
        if (Content is not StackPanel stack || stack.Children.Count < 2 || stack.Children[1] is not ProgressBar bar) return;
        if (Value > 85) bar.Foreground = new SolidColorBrush(Color.FromRgb(230, 67, 67));
        else if (Value > 60) bar.Foreground = new SolidColorBrush(Color.FromRgb(224, 126, 23));
        else bar.SetResourceReference(ProgressBar.ForegroundProperty, "Accent");
    }
}
