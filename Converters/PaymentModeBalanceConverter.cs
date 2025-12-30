using Microsoft.UI.Xaml.Data;
using System;
using Microsoft.Extensions.DependencyInjection;
using Expense_Flow.Services;
using Expense_Flow.Models;

namespace Expense_Flow.Converters;

public class PaymentModeBalanceConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is PaymentMode paymentMode)
        {
            try
            {
                var settingsService = App.Host?.Services?.GetService<ISettingsService>();
                var symbol = settingsService?.GetCurrencySymbol() ?? "$";

                return paymentMode.Type switch
                {
                    PaymentModeType.Card => paymentMode.DisplayNumber,
                    PaymentModeType.UPI => paymentMode.DisplayNumber,
                    PaymentModeType.Cash => $"{symbol}{paymentMode.DisplayNumber}",
                    _ => paymentMode.DisplayNumber
                };
            }
            catch
            {
                return paymentMode.Type switch
                {
                    PaymentModeType.Card => paymentMode.DisplayNumber,
                    PaymentModeType.UPI => paymentMode.DisplayNumber,
                    PaymentModeType.Cash => $"${paymentMode.DisplayNumber}",
                    _ => paymentMode.DisplayNumber
                };
            }
        }
        return value?.ToString() ?? "";
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
