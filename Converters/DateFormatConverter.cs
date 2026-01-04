using Microsoft.UI.Xaml.Data;
using System;
using Microsoft.Extensions.DependencyInjection;
using Expense_Flow.Services;

namespace Expense_Flow.Converters;

public class DateFormatConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is DateTime dateTime)
        {
            var format = parameter as string ?? "MMM dd, yyyy";
            return dateTime.ToString(format);
        }
        return string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}

public class CurrencyAmountConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is decimal amount)
        {
            try
            {
                var settingsService = App.Host?.Services?.GetService<ISettingsService>();
                if (settingsService != null)
                {
                    return settingsService.FormatCurrency(amount);
                }
                return $"${amount:N2}";
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
