using Microsoft.UI.Xaml.Data;
using System;
using Microsoft.Extensions.DependencyInjection;
using Expense_Flow.Services;
using Expense_Flow.Models;

namespace Expense_Flow.Converters;

public class PaymentModeTypeToCurrencyConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is PaymentModeType type && type == PaymentModeType.Cash)
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
        return string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
