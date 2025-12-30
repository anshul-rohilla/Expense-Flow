using Microsoft.UI.Xaml.Data;
using System;
using Microsoft.Extensions.DependencyInjection;
using Expense_Flow.Services;

namespace Expense_Flow.Converters;

public class CurrencyFormatterConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        try
        {
            var settingsService = App.Host?.Services?.GetService<ISettingsService>();
            var currencySymbol = settingsService?.GetCurrencySymbol() ?? "$";

            if (value is decimal decimalValue)
            {
                return $"{currencySymbol}{decimalValue:N2}";
            }

            return $"{currencySymbol}0.00";
        }
        catch
        {
            return "$0.00";
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
