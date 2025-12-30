using Microsoft.UI.Xaml;

namespace Expense_Flow.Services;

public interface ISettingsService
{
    string Currency { get; set; }
    string DateFormat { get; set; }
    ElementTheme Theme { get; set; }
    string Backdrop { get; set; }
    bool LaunchOnStartup { get; set; }

    void SaveSettings();
    void LoadSettings();
    string GetCurrencySymbol();
    string FormatCurrency(decimal amount);
}
