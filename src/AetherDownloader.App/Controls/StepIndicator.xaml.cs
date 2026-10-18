using System.Windows;
using System.Windows.Controls;

namespace AetherDownloader.App.Controls;

public partial class StepIndicator : UserControl
{
    public static readonly DependencyProperty CurrentStepProperty =
        DependencyProperty.Register(
            nameof(CurrentStep),
            typeof(int),
            typeof(StepIndicator),
            new PropertyMetadata(1));

    public int CurrentStep
    {
        get => (int)GetValue(CurrentStepProperty);
        set => SetValue(CurrentStepProperty, value);
    }

    public StepIndicator()
    {
        InitializeComponent();
    }
}
