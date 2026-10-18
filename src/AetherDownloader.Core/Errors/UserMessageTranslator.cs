using System.Globalization;
using System.Resources;
using AetherDownloader.Core.Constants;
using AetherDownloader.Core.Models;
using AetherDownloader.Core.Process;

namespace AetherDownloader.Core.Errors;

public sealed class UserMessageTranslator
{
    private const string ResourceBaseName = "AetherDownloader.Core.Errors.ErrorMessages";

    private static readonly ResourceManager MessageResources = new(
        ResourceBaseName,
        typeof(UserMessageTranslator).Assembly);

    public UserFacingError Translate(
        ErrorCode code,
        IReadOnlyList<RecoveryAction> recoveryActions,
        string? technicalDetail = null)
    {
        var (titleFa, messageFa) = LoadFa(code);
        var (titleEn, messageEn) = LoadEn(code);

        return new UserFacingError
        {
            Code = code,
            TitleFa = titleFa,
            MessageFa = messageFa,
            TitleEn = titleEn,
            MessageEn = messageEn,
            TechnicalDetail = technicalDetail,
            RecoveryActions = recoveryActions,
        };
    }

    public UserFacingError FromProcessResult(ProcessRunResult result, YtDlpErrorClassifier classifier)
    {
        var code = classifier.Classify(result);
        var actions = GetRecoveryActions(code);
        var detail = string.Join('\n', result.StandardError.TakeLast(5));
        return Translate(code, actions, detail);
    }

    private static IReadOnlyList<RecoveryAction> GetRecoveryActions(ErrorCode code)
    {
        foreach (var (ruleCode, _, primaryAction) in DownloadErrorPatterns.Rules)
        {
            if (ruleCode == code)
            {
                return [primaryAction];
            }
        }

        return code switch
        {
            ErrorCode.DlNetwork => [RecoveryAction.Retry],
            ErrorCode.DlGeo => [RecoveryAction.Settings],
            ErrorCode.DlCookie => [RecoveryAction.CookieGuide],
            ErrorCode.DlFormat => [RecoveryAction.ReInspect],
            ErrorCode.DlDisk => [RecoveryAction.ChangePath],
            ErrorCode.DlFfmpeg => [RecoveryAction.Settings],
            _ => [RecoveryAction.Retry, RecoveryAction.CopyLog],
        };
    }

    private static (string Title, string Message) LoadFa(ErrorCode code)
    {
        var prefix = code.ToString();
        var culture = CultureInfo.GetCultureInfo("fa");
        var title = MessageResources.GetString($"{prefix}_Title", culture) ?? "خطای دانلود";
        var message = MessageResources.GetString($"{prefix}_Message", culture) ?? "دانلود ناموفق بود.";
        return (title, message);
    }

    private static (string Title, string Message) LoadEn(ErrorCode code)
    {
        var prefix = code.ToString();
        var culture = CultureInfo.GetCultureInfo("en");
        var title = MessageResources.GetString($"{prefix}_Title", culture) ?? "Download failed";
        var message = MessageResources.GetString($"{prefix}_Message", culture) ?? "Download did not complete.";
        return (title, message);
    }
}
