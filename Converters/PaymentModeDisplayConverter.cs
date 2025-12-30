using Microsoft.UI.Xaml.Data;
using System;
using Microsoft.Extensions.DependencyInjection;
using Expense_Flow.Services;
using Expense_Flow.Models;

namespace Expense_Flow.Converters;

public class PaymentModeDisplayConverter : IValueConverter
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
                    PaymentModeType.Card => $"•••• •••• •••• {paymentMode.LastFourDigits ?? "****"}",
                    PaymentModeType.UPI => paymentMode.UpiId ?? "Not Set",
                    PaymentModeType.Cash => paymentMode.Balance.HasValue ? $"{symbol}{paymentMode.Balance.Value:N2}" : $"{symbol}0.00",
                    _ => "N/A"
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"PaymentModeDisplayConverter error: {ex.Message}");
                return paymentMode.Type switch
                {
                    PaymentModeType.Card => $"•••• •••• •••• {paymentMode.LastFourDigits ?? "****"}",
                    PaymentModeType.UPI => paymentMode.UpiId ?? "Not Set",
                    PaymentModeType.Cash => paymentMode.Balance.HasValue ? $"${paymentMode.Balance.Value:N2}" : "$0.00",
                    _ => "N/A"
                };
            }
        }

        // Debug output to see what we're receiving
        System.Diagnostics.Debug.WriteLine($"PaymentModeDisplayConverter received: {value?.GetType()?.Name ?? "null"}");
        return value?.ToString() ?? "N/A";
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
