using Microsoft.UI.Xaml.Controls;
using Microsoft.Extensions.DependencyInjection;
using Expense_Flow.ViewModels;
using Expense_Flow.Models;
using Expense_Flow.Services;
using System;
using Microsoft.UI.Xaml;

namespace Expense_Flow.Views.PaymentModes;

public sealed partial class PaymentModesPage : Page
{
    public PaymentModesViewModel ViewModel { get; }

    public PaymentModesPage()
    {
        InitializeComponent();
        ViewModel = App.Host!.Services.GetRequiredService<PaymentModesViewModel>();
        DataContext = ViewModel;
        Loaded += PaymentModesPage_Loaded;
    }

    // Helper method for x:Bind to format payment mode display with currency
    public string FormatPaymentModeDisplay(PaymentMode paymentMode)
    {
        if (paymentMode == null) return string.Empty;

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
            return paymentMode.DisplayNumber;
        }
    }

    private async void PaymentModesPage_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        await ViewModel.LoadPaymentModesCommand.ExecuteAsync(null);
    }

    private void FilterByType_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string typeString)
        {
            if (typeString == "All")
            {
                ViewModel.FilterByTypeCommand.Execute(null);
            }
            else if (System.Enum.TryParse<PaymentModeType>(typeString, out var type))
            {
                ViewModel.FilterByTypeCommand.Execute(type);
            }

            // Update button styles to reflect active filter
            var primaryStyle = (Microsoft.UI.Xaml.Style)Application.Current.Resources["PrimaryButtonStyle"];
            FilterAllButton.Style = (typeString == "All") ? primaryStyle : null;
            FilterCardButton.Style = (typeString == "Card") ? primaryStyle : null;
            FilterCashButton.Style = (typeString == "Cash") ? primaryStyle : null;
            FilterUpiButton.Style = (typeString == "UPI") ? primaryStyle : null;
        }
    }

    private async void AddPaymentMode_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        var dialog = new PaymentModeDialog
        {
            XamlRoot = this.XamlRoot
        };

        var result = await dialog.ShowAsync();
        
        if (result == ContentDialogResult.Primary && dialog.PaymentMode != null)
        {
            var paymentModeService = App.Host!.Services.GetRequiredService<Services.IPaymentModeService>();
            var createResult = await paymentModeService.CreatePaymentModeAsync(dialog.PaymentMode);

            if (createResult.Success)
            {
                await ViewModel.LoadPaymentModesCommand.ExecuteAsync(null);
            }
            else
            {
                var errorDialog = new ContentDialog
                {
                    Title = "Error",
                    Content = createResult.GetErrorMessage(),
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                };
                await errorDialog.ShowAsync();
            }
        }
    }

    private async void EditPaymentMode_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is PaymentMode paymentMode)
        {
            var dialog = new PaymentModeDialog(paymentMode)
            {
                XamlRoot = this.XamlRoot
            };

            var result = await dialog.ShowAsync();
            
            if (result == ContentDialogResult.Primary && dialog.PaymentMode != null)
            {
                var paymentModeService = App.Host!.Services.GetRequiredService<Services.IPaymentModeService>();
                var updateResult = await paymentModeService.UpdatePaymentModeAsync(dialog.PaymentMode);

                if (updateResult.Success)
                {
                    await ViewModel.LoadPaymentModesCommand.ExecuteAsync(null);
                }
                else
                {
                    var errorDialog = new ContentDialog
                    {
                        Title = "Error",
                        Content = updateResult.GetErrorMessage(),
                        CloseButtonText = "OK",
                        XamlRoot = this.XamlRoot
                    };
                    await errorDialog.ShowAsync();
                }
            }
        }
    }

    private async void DeletePaymentMode_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is PaymentMode paymentMode)
        {
            var confirmDialog = new ContentDialog
            {
                Title = "Delete Payment Mode",
                Content = $"Are you sure you want to delete '{paymentMode.Name}'? This action cannot be undone.",
                PrimaryButtonText = "Delete",
                SecondaryButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Secondary,
                XamlRoot = this.XamlRoot
            };

            var result = await confirmDialog.ShowAsync();
            
            if (result == ContentDialogResult.Primary)
            {
                await ViewModel.DeletePaymentModeCommand.ExecuteAsync(paymentMode);
            }
        }
    }
}
