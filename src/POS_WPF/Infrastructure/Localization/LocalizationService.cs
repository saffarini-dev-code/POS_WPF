using System.Globalization;
using System.Windows;

namespace POS_WPF.Infrastructure.Localization;

public enum AppLanguage { English, Arabic }

public sealed class LocalizationService
{
    public AppLanguage CurrentLanguage { get; private set; } = AppLanguage.English;
    public CultureInfo Culture => CurrentLanguage == AppLanguage.Arabic ? CultureInfo.GetCultureInfo("ar") : CultureInfo.GetCultureInfo("en-US");
    public FlowDirection FlowDirection => CurrentLanguage == AppLanguage.Arabic ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;

    public void SetLanguage(AppLanguage language)
    {
        CurrentLanguage = language;
        CultureInfo.CurrentCulture = Culture;
        CultureInfo.CurrentUICulture = Culture;
    }
}
