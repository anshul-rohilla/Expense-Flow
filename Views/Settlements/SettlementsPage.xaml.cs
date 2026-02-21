using Microsoft.UI.Xaml.Controls;
using Microsoft.Extensions.DependencyInjection;
using Expense_Flow.ViewModels;
using Expense_Flow.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Expense_Flow.Views.Settlements;

public sealed partial class SettlementsPage : Page
{
    public SettlementsViewModel ViewModel { get; }

    public SettlementsPage()
    {
        InitializeComponent();
        ViewModel = App.Host!.Services.GetRequiredService<SettlementsViewModel>();
        DataContext = ViewModel;
        Loaded += SettlementsPage_Loaded;
    }

    private async void SettlementsPage_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        await ViewModel.LoadAllDataCommand.ExecuteAsync(null);
    }

    private async void NewSettlement_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        var dialog = new SettlementDialog(ViewModel.UnsettledExpenses.ToList());
        dialog.XamlRoot = this.XamlRoot;

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary && dialog.SelectedExpenseIds.Any())
        {
            var settlementService = App.Host!.Services.GetRequiredService<Services.ISettlementService>();
            var orgService = App.Host!.Services.GetRequiredService<Services.IOrganizationService>();
            var orgId = orgService.GetCurrentOrganizationId();

            var settlement = new Settlement
            {
                OrganizationId = orgId,
                Reference = dialog.SettlementReference,
                ContactId = dialog.SettlementContactId ?? 1,
                PaymentModeId = dialog.SettlementPaymentModeId,
                Currency = dialog.SettlementCurrency ?? "USD",
                TransactionReference = dialog.TransactionReference
            };

            var createResult = await settlementService.CreateSettlementAsync(
                settlement, dialog.SelectedExpenseIds.ToList());

            if (createResult.Success)
            {
                // Auto-complete the settlement
                if (createResult.Data != null)
                {
                    await settlementService.CompleteSettlementAsync(createResult.Data.Id, dialog.SettlementPaymentModeId, dialog.TransactionReference);
                }
                await ViewModel.LoadAllDataCommand.ExecuteAsync(null);
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

    private async void CancelSettlement_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is Settlement settlement)
        {
            var confirmDialog = new ContentDialog
            {
                Title = "Cancel Settlement",
                Content = $"Are you sure you want to cancel settlement '{settlement.Reference}'? " +
                          "Expenses will return to unsettled status.",
                PrimaryButtonText = "Cancel Settlement",
                SecondaryButtonText = "Keep",
                DefaultButton = ContentDialogButton.Secondary,
                XamlRoot = this.XamlRoot
            };

            var result = await confirmDialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                await ViewModel.CancelSettlementCommand.ExecuteAsync(settlement);
            }
        }
    }
}
