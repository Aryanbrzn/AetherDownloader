using System.Windows;

namespace AetherDownloader.App.Services;

public sealed class DialogService : IDialogService
{
    public void ShowError(string title, string message) =>
        MessageBox.Show(
            message,
            title,
            MessageBoxButton.OK,
            MessageBoxImage.Error);

    public void ShowInfo(string title, string message) =>
        MessageBox.Show(
            message,
            title,
            MessageBoxButton.OK,
            MessageBoxImage.Information);

    public bool? ShowDialog(Window dialog)
    {
        dialog.Owner = Application.Current.MainWindow;
        return dialog.ShowDialog();
    }

    public MessageBoxResult ShowConfirm(string title, string message, MessageBoxButton buttons = MessageBoxButton.YesNoCancel) =>
        MessageBox.Show(
            message,
            title,
            buttons,
            MessageBoxImage.Question);
}
