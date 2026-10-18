using System.Windows;
using AetherDownloader.App.ViewModels;

namespace AetherDownloader.App.Views;

public partial class CookieGuideModal : Window
{
    public CookieGuideModal(CookieGuideViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;

        Action? closeHandler = null;
        closeHandler = () =>
        {
            viewModel.CloseRequested -= closeHandler!;
            Close();
        };
        viewModel.CloseRequested += closeHandler;
    }
}
