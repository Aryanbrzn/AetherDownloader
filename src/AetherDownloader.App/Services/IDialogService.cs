using System.Windows;

namespace AetherDownloader.App.Services;

public interface IDialogService
{
    void ShowError(string title, string message);

    void ShowInfo(string title, string message);

    bool? ShowDialog(Window dialog);

    MessageBoxResult ShowConfirm(string title, string message, MessageBoxButton buttons = MessageBoxButton.YesNoCancel);
}
