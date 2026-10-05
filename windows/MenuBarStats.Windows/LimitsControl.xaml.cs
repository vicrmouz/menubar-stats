using System.Windows;
using System.Windows.Controls;
using MenuBarStats.Core;
namespace MenuBarStats.Windows;
public partial class LimitsControl : UserControl
{
    public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(nameof(Title), typeof(string), typeof(LimitsControl), new PropertyMetadata("", Changed));
    public static readonly DependencyProperty LimitsProperty = DependencyProperty.Register(nameof(Limits), typeof(UsageLimits), typeof(LimitsControl), new PropertyMetadata(null, Changed));
    public string Title { get => (string)GetValue(TitleProperty); set => SetValue(TitleProperty, value); }
    public UsageLimits? Limits { get => (UsageLimits?)GetValue(LimitsProperty); set => SetValue(LimitsProperty, value); }
    public LimitsControl() { InitializeComponent(); Loaded += (_, _) => Update(); }
    private static void Changed(DependencyObject d, DependencyPropertyChangedEventArgs e) => ((LimitsControl)d).Update();
    private void Update()
    {
        if (Heading is null) return; Heading.Text = Title;
        Empty.Visibility = Limits is null ? Visibility.Visible : Visibility.Collapsed; Meters.Visibility = Limits is null ? Visibility.Collapsed : Visibility.Visible;
        if (Limits is { } limits) { Session.Value = limits.Session.Percent; Session.Detail = Detail(limits.Session); Weekly.Value = limits.Weekly.Percent; Weekly.Detail = Detail(limits.Weekly); }
    }
    private static string Detail(LimitWindow window) => $"{window.Percent:0}%" + (window.ResetsAt is { } reset ? " · reinicia " + StatsViewModel.Relative(reset) : "");
}
