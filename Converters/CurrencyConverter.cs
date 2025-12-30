using Microsoft.UI.Xaml.Data;
using System;
using Microsoft.Extensions.DependencyInjection;
using Expense_Flow.Services;

namespace Expense_Flow.Converters;

public class CurrencyConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is decimal amount)
        {
            try
            {
                var settingsService = App.Host?.Services?.GetService<ISettingsService>();
                var symbol = settingsService?.GetCurrencySymbol() ?? "$";
                return $"{symbol}{amount:N2}";
            }
            catch
            {
                return $"${amount:N2}";
            }
        }
        return "$0.00";
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
