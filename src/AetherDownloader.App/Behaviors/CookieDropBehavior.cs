using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace AetherDownloader.App.Behaviors;

public static class CookieDropBehavior
{
    public static readonly DependencyProperty IsEnabledProperty =
        DependencyProperty.RegisterAttached(
            "IsEnabled",
            typeof(bool),
            typeof(CookieDropBehavior),
            new PropertyMetadata(false, OnIsEnabledChanged));

    public static readonly DependencyProperty DropCommandProperty =
        DependencyProperty.RegisterAttached(
            "DropCommand",
            typeof(ICommand),
            typeof(CookieDropBehavior),
            new PropertyMetadata(null));

    public static readonly DependencyProperty IsDragOverProperty =
        DependencyProperty.RegisterAttached(
            "IsDragOver",
            typeof(bool),
            typeof(CookieDropBehavior),
            new PropertyMetadata(false));

    public static bool GetIsEnabled(DependencyObject obj) => (bool)obj.GetValue(IsEnabledProperty);

    public static void SetIsEnabled(DependencyObject obj, bool value) => obj.SetValue(IsEnabledProperty, value);

    public static ICommand? GetDropCommand(DependencyObject obj) => (ICommand?)obj.GetValue(DropCommandProperty);

    public static void SetDropCommand(DependencyObject obj, ICommand? value) => obj.SetValue(DropCommandProperty, value);

    public static bool GetIsDragOver(DependencyObject obj) => (bool)obj.GetValue(IsDragOverProperty);

    public static void SetIsDragOver(DependencyObject obj, bool value) => obj.SetValue(IsDragOverProperty, value);

    private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not UIElement element)
        {
            return;
        }

        if (e.NewValue is true)
        {
            element.AllowDrop = true;
            element.DragOver += OnDragOver;
            element.DragLeave += OnDragLeave;
            element.Drop += OnDrop;
        }
        else
        {
            element.AllowDrop = false;
            element.DragOver -= OnDragOver;
            element.DragLeave -= OnDragLeave;
            element.Drop -= OnDrop;
        }
    }

    private static void OnDragOver(object sender, DragEventArgs e)
    {
        if (sender is not DependencyObject target)
        {
            return;
        }

        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            var files = (string[]?)e.Data.GetData(DataFormats.FileDrop);
            if (files?.Any(static f => f.EndsWith(".txt", StringComparison.OrdinalIgnoreCase)) == true)
            {
                e.Effects = DragDropEffects.Copy;
                e.Handled = true;
                SetIsDragOver(target, true);
                return;
            }
        }

        e.Effects = DragDropEffects.None;
        e.Handled = true;
        SetIsDragOver(target, false);
    }

    private static void OnDragLeave(object sender, DragEventArgs e)
    {
        if (sender is DependencyObject target)
        {
            SetIsDragOver(target, false);
        }
    }

    private static void OnDrop(object sender, DragEventArgs e)
    {
        if (sender is not DependencyObject target)
        {
            return;
        }

        SetIsDragOver(target, false);

        if (!e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            return;
        }

        var files = (string[]?)e.Data.GetData(DataFormats.FileDrop);
        var txtPath = files?.FirstOrDefault(static f => f.EndsWith(".txt", StringComparison.OrdinalIgnoreCase));
        if (txtPath is null)
        {
            return;
        }

        var command = GetDropCommand(target);
        if (command?.CanExecute(txtPath) == true)
        {
            command.Execute(txtPath);
        }

        e.Handled = true;
    }
}
