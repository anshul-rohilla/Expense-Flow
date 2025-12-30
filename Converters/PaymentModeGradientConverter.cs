using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using System;
using Expense_Flow.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI;

namespace Expense_Flow.Converters;

public class PaymentModeGradientConverter : IValueConverter
{
    // Define gradients directly in the converter to avoid resource lookup issues
    private static readonly LinearGradientBrush DefaultCardGradient = new()
    {
        StartPoint = new Windows.Foundation.Point(0, 0),
        EndPoint = new Windows.Foundation.Point(1, 1),
        GradientStops =
        {
            new GradientStop { Color = ColorHelper.FromArgb(255, 102, 126, 234), Offset = 0 },
            new GradientStop { Color = ColorHelper.FromArgb(255, 118, 75, 162), Offset = 1 }
        }
    };

    private static readonly LinearGradientBrush CashGradient = new()
    {
        StartPoint = new Windows.Foundation.Point(0, 0),
        EndPoint = new Windows.Foundation.Point(1, 1),
        GradientStops =
        {
            new GradientStop { Color = ColorHelper.FromArgb(255, 17, 153, 142), Offset = 0 },
            new GradientStop { Color = ColorHelper.FromArgb(255, 56, 239, 125), Offset = 1 }
        }
    };

    private static readonly LinearGradientBrush UpiGradient = new()
    {
        StartPoint = new Windows.Foundation.Point(0, 0),
        EndPoint = new Windows.Foundation.Point(1, 1),
        GradientStops =
        {
            new GradientStop { Color = ColorHelper.FromArgb(255, 240, 147, 251), Offset = 0 },
            new GradientStop { Color = ColorHelper.FromArgb(255, 245, 87, 108), Offset = 1 }
        }
    };

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is PaymentModeType type)
        {
            return type switch
            {
                PaymentModeType.Cash => CashGradient,
                PaymentModeType.UPI => UpiGradient,
                PaymentModeType.Card => DefaultCardGradient,
                _ => DefaultCardGradient
            };
        }
        return DefaultCardGradient;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
