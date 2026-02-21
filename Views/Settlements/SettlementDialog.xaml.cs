using Microsoft.UI.Xaml.Controls;
using Expense_Flow.Models;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Expense_Flow.Views.Settlements;

public sealed partial class SettlementDialog : ContentDialog
{
    public List<int> SelectedExpenseIds { get; private set; } = new();
    public string SettlementReference { get; private set; } = string.Empty;
    public int? SettlementContactId { get; private set; }
    public int? SettlementPaymentModeId { get; private set; }
    public string? SettlementCurrency { get; private set; }
    public string? TransactionReference { get; private set; }

    public ObservableCollection<PaymentMode> PaymentModes { get; } = new();
    private readonly List<Expense> _unsettledExpenses;

    public SettlementDialog(List<Expense> unsettledExpenses)
    {
        InitializeComponent();
        _unsettledExpenses = unsettledExpenses;

        // Generate default reference
        ReferenceTextBox.Text = $"SETTLE-{DateTime.Now:yyyyMMdd}-{DateTime.Now:HHmm}";

        // Populate expenses list
        ExpenseListView.ItemsSource = _unsettledExpenses;

        LoadPaymentModesAsync();
        UpdateTotalAmount();
    }

    private async void LoadPaymentModesAsync()
    {
        try
        {
            var paymentModeService = App.Host!.Services.GetRequiredService<Services.IPaymentModeService>();
            var orgService = App.Host!.Services.GetRequiredService<Services.IOrganizationService>();
            var orgId = orgService.GetCurrentOrganizationId();
            var result = await paymentModeService.GetAllPaymentModesAsync(orgId);

            if (result.Success && result.Data != null)
            {
                PaymentModes.Clear();
                foreach (var pm in result.Data)
                {
                    PaymentModes.Add(pm);
                }

                if (PaymentModes.Count > 0)
                {
                    PaymentModeComboBox.SelectedIndex = 0;
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading payment modes: {ex.Message}");
        }
    }

    private void ExpenseListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdateTotalAmount();
    }

    private void UpdateTotalAmount()
    {
        var selectedExpenses = ExpenseListView.SelectedItems.Cast<Expense>().ToList();
        var total = selectedExpenses.Sum(exp => exp.PendingReimbursement);
        TotalAmountText.Text = $"Total: {total:C2} ({selectedExpenses.Count} expense{(selectedExpenses.Count != 1 ? "s" : "")})";
    }

    private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        ErrorTextBlock.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;

        if (string.IsNullOrWhiteSpace(ReferenceTextBox.Text))
        {
            args.Cancel = true;
            ErrorTextBlock.Text = "Settlement reference is required.";
            ErrorTextBlock.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
            return;
        }

        var selectedExpenses = ExpenseListView.SelectedItems.Cast<Expense>().ToList();
        if (!selectedExpenses.Any())
        {
            args.Cancel = true;
            ErrorTextBlock.Text = "Please select at least one expense to settle.";
            ErrorTextBlock.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
            return;
        }

        SettlementReference = ReferenceTextBox.Text.Trim();
        SelectedExpenseIds = selectedExpenses.Select(e => e.Id).ToList();
        TransactionReference = string.IsNullOrWhiteSpace(TransactionReferenceTextBox.Text)
            ? null : TransactionReferenceTextBox.Text.Trim();

        if (PaymentModeComboBox.SelectedItem is PaymentMode pm)
        {
            SettlementPaymentModeId = pm.Id;
        }

        // Use app's default currency
        var settingsService = App.Host!.Services.GetRequiredService<Services.ISettingsService>();
        SettlementCurrency = settingsService.Currency ?? "INR";
    }

    private void ContentDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        SelectedExpenseIds.Clear();
    }
}
