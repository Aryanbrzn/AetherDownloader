using System.Windows;
using System.Windows.Controls;

namespace AetherDownloader.App.Views;

public partial class ZonePlaceholder : UserControl
{
    public static readonly DependencyProperty ZoneNameProperty =
        DependencyProperty.Register(
            nameof(ZoneName),
            typeof(string),
            typeof(ZonePlaceholder),
            new PropertyMetadata(string.Empty));

    public string ZoneName
    {
        get => (string)GetValue(ZoneNameProperty);
        set => SetValue(ZoneNameProperty, value);
    }

    public ZonePlaceholder()
    {
        InitializeComponent();
    }
}
