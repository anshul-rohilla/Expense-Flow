using Microsoft.UI.Xaml.Controls;
using Microsoft.Extensions.DependencyInjection;
using Expense_Flow.ViewModels;
using Expense_Flow.Models;
using System;

namespace Expense_Flow.Views.Subscriptions;

public sealed partial class SubscriptionsPage : Page
{
    public SubscriptionsViewModel ViewModel { get; }

    public SubscriptionsPage()
    {
        InitializeComponent();
        ViewModel = App.Host!.Services.GetRequiredService<SubscriptionsViewModel>();
        DataContext = ViewModel;
        Loaded += SubscriptionsPage_Loaded;
    }

    private async void SubscriptionsPage_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        await ViewModel.LoadSubscriptionsCommand.ExecuteAsync(null);
    }

    private async void AddSubscription_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        var dialog = new SubscriptionDialog();
        dialog.XamlRoot = this.XamlRoot;

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary && dialog.Subscription != null)
        {
            var SubscriptionService = App.Host!.Services.GetRequiredService<Services.ISubscriptionService>();
            var createResult = await SubscriptionService.CreateSubscriptionAsync(dialog.Subscription);

            if (createResult.Success)
            {
                await ViewModel.LoadSubscriptionsCommand.ExecuteAsync(null);
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

    private async void EditSubscription_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is Subscription Subscription)
        {
            var dialog = new SubscriptionDialog(Subscription);
            dialog.XamlRoot = this.XamlRoot;

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary && dialog.Subscription != null)
            {
                var SubscriptionService = App.Host!.Services.GetRequiredService<Services.ISubscriptionService>();
                var updateResult = await SubscriptionService.UpdateSubscriptionAsync(dialog.Subscription);

                if (updateResult.Success)
                {
                    await ViewModel.LoadSubscriptionsCommand.ExecuteAsync(null);
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

    private async void DeleteSubscription_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is Subscription Subscription)
        {
            var confirmDialog = new ContentDialog
            {
                Title = "Delete Subscription",
                Content = $"Are you sure you want to delete '{Subscription.Name}'? This action cannot be undone.",
                PrimaryButtonText = "Delete",
                SecondaryButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Secondary,
                XamlRoot = this.XamlRoot
            };

            var result = await confirmDialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                await ViewModel.DeleteSubscriptionCommand.ExecuteAsync(Subscription);
            }
        }
    }
}
