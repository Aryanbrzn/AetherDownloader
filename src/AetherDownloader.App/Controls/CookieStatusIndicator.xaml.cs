using System.Windows;
using System.Windows.Controls;
using AetherDownloader.Core.Models;

namespace AetherDownloader.App.Controls;

public partial class CookieStatusIndicator : UserControl
{
    public static readonly DependencyProperty StatusProperty =
        DependencyProperty.Register(
            nameof(Status),
            typeof(CookieStatus),
            typeof(CookieStatusIndicator),
            new PropertyMetadata(CookieStatus.Missing));

    public CookieStatus Status
    {
        get => (CookieStatus)GetValue(StatusProperty);
        set => SetValue(StatusProperty, value);
    }

    public CookieStatusIndicator()
    {
        InitializeComponent();
    }
}
