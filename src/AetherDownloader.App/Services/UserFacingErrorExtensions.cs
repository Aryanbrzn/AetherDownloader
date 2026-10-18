using AetherDownloader.Core.Models;

namespace AetherDownloader.App.Services;

public static class UserFacingErrorExtensions
{
    public static string GetMessage(UserFacingError? error, AppLanguage language) =>
        error is null
            ? string.Empty
            : language == AppLanguage.Fa ? error.MessageFa : error.MessageEn;

    public static string GetTitle(UserFacingError? error, AppLanguage language) =>
        error is null
            ? string.Empty
            : language == AppLanguage.Fa ? error.TitleFa : error.TitleEn;
}
