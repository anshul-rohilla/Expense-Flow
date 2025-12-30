using Microsoft.UI.Xaml;
using Windows.Storage;
using System.Collections.Generic;

namespace Expense_Flow.Services;

public class SettingsService : ISettingsService
{
    private readonly ApplicationDataContainer _localSettings;
    private const string CURRENCY_KEY = "AppCurrency";
    private const string DATE_FORMAT_KEY = "DateFormat";
    private const string THEME_KEY = "AppTheme";
    private const string BACKDROP_KEY = "AppBackdrop";
    private const string LAUNCH_ON_STARTUP_KEY = "LaunchOnStartup";

    private static readonly Dictionary<string, string> CurrencySymbols = new()
    {
        { "USD", "$" },
        { "EUR", "\u20AC" },  // €
        { "GBP", "\u00A3" },  // £
        { "INR", "\u20B9" },  // ?
        { "JPY", "\u00A5" },  // ¥
        { "AUD", "A$" },
        { "CAD", "C$" },
        { "CHF", "CHF" }
    };

    public SettingsService()
    {
        _localSettings = ApplicationData.Current.LocalSettings;
        LoadSettings();
    }

    public string Currency { get; set; } = "USD";
    public string DateFormat { get; set; } = "MM/DD/YYYY";
    public ElementTheme Theme { get; set; } = ElementTheme.Default;
    public string Backdrop { get; set; } = "Mica";
    public bool LaunchOnStartup { get; set; } = false;

    public void LoadSettings()
    {
        Currency = _localSettings.Values[CURRENCY_KEY] as string ?? "USD";
        DateFormat = _localSettings.Values[DATE_FORMAT_KEY] as string ?? "MM/DD/YYYY";
        
        var themeValue = _localSettings.Values[THEME_KEY] as string ?? "Default";
        Theme = themeValue switch
        {
            "Light" => ElementTheme.Light,
            "Dark" => ElementTheme.Dark,
            _ => ElementTheme.Default
        };

        Backdrop = _localSettings.Values[BACKDROP_KEY] as string ?? "Mica";
        LaunchOnStartup = _localSettings.Values[LAUNCH_ON_STARTUP_KEY] as bool? ?? false;
    }

    public void SaveSettings()
    {
        _localSettings.Values[CURRENCY_KEY] = Currency;
        _localSettings.Values[DATE_FORMAT_KEY] = DateFormat;
        
        var themeString = Theme switch
        {
            ElementTheme.Light => "Light",
            ElementTheme.Dark => "Dark",
            _ => "Default"
        };
        _localSettings.Values[THEME_KEY] = themeString;
        
        _localSettings.Values[BACKDROP_KEY] = Backdrop;
        _localSettings.Values[LAUNCH_ON_STARTUP_KEY] = LaunchOnStartup;
    }

    public string GetCurrencySymbol()
    {
        return CurrencySymbols.TryGetValue(Currency, out var symbol) ? symbol : "$";
    }

    public string FormatCurrency(decimal amount)
    {
        var symbol = GetCurrencySymbol();
        return $"{symbol}{amount:N2}";
    }
}
