using Microsoft.UI.Xaml.Data;
using System;
using Microsoft.Extensions.DependencyInjection;
using Expense_Flow.Services;

namespace Expense_Flow.Converters;

public class CurrencySymbolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        try
        {
            var settingsService = App.Host?.Services?.GetService<ISettingsService>();
            return settingsService?.GetCurrencySymbol() ?? "$";
        }
        catch
        {
            return "$";
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
