using Microsoft.UI.Xaml.Controls;
using Expense_Flow.Models;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace Expense_Flow.Views.Subscriptions;

public sealed partial class SubscriptionDialog : ContentDialog
{
    public Subscription Subscription { get; private set; }
    private bool _isEditMode;
    public ObservableCollection<Vendor> Vendors { get; } = new();

    public SubscriptionDialog(Subscription? subscription = null)
    {
        InitializeComponent();
        _isEditMode = subscription != null;
        Title = _isEditMode ? "Edit Subscription" : "Add Subscription";
        PrimaryButtonText = _isEditMode ? "Update" : "Create";
        SecondaryButtonText = "Cancel";

        Subscription = subscription ?? new Subscription();

        LoadVendorsAsync();

        // Set billing cycle to Monthly by default
        BillingCycleComboBox.SelectedIndex = 0;

        // Set currency to app default
        var settingsService = App.Host!.Services.GetRequiredService<Services.ISettingsService>();
        var defaultCurrency = settingsService.Currency ?? "INR";
        SelectCurrency(_isEditMode && subscription != null ? (subscription.Currency ?? defaultCurrency) : defaultCurrency);

        if (_isEditMode && subscription != null)
        {
            NameTextBox.Text = subscription.Name ?? string.Empty;
            PlanTextBox.Text = subscription.Plan ?? string.Empty;
            ReferenceTextBox.Text = subscription.Reference ?? string.Empty;
            NotesTextBox.Text = subscription.Notes ?? string.Empty;

            if (subscription.Amount.HasValue)
            {
                AmountNumberBox.Value = (double)subscription.Amount.Value;
            }

            // Set billing cycle
            var billingCycleTag = subscription.BillingCycle.ToString();
            for (int i = 0; i < BillingCycleComboBox.Items.Count; i++)
            {
                if (BillingCycleComboBox.Items[i] is ComboBoxItem item &&
                    item.Tag?.ToString() == billingCycleTag)
                {
                    BillingCycleComboBox.SelectedIndex = i;
                    break;
                }
            }
        }
    }

    private void SelectCurrency(string currencyCode)
    {
        for (int i = 0; i < CurrencyComboBox.Items.Count; i++)
        {
            if (CurrencyComboBox.Items[i] is ComboBoxItem item &&
                item.Tag?.ToString() == currencyCode)
            {
                CurrencyComboBox.SelectedIndex = i;
                return;
            }
        }
        CurrencyComboBox.SelectedIndex = 0; // Default to first (INR)
    }

    private async void LoadVendorsAsync()
    {
        try
        {
            var vendorService = App.Host!.Services.GetRequiredService<Services.IVendorService>();
            var orgService = App.Host!.Services.GetRequiredService<Services.IOrganizationService>();
            var orgId = orgService.GetCurrentOrganizationId();
            var result = await vendorService.GetAllVendorsAsync(orgId);

            if (result.Success && result.Data != null)
            {
                Vendors.Clear();
                foreach (var vendor in result.Data)
                {
                    Vendors.Add(vendor);
                }

                if (_isEditMode && Subscription.VendorId > 0)
                {
                    VendorComboBox.SelectedItem = Vendors.FirstOrDefault(v => v.Id == Subscription.VendorId);
                }
                else if (Vendors.Count > 0)
                {
                    VendorComboBox.SelectedIndex = 0;
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading vendors: {ex.Message}");
        }
    }

    private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        ErrorTextBlock.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;

        if (string.IsNullOrWhiteSpace(NameTextBox.Text))
        {
            args.Cancel = true;
            ErrorTextBlock.Text = "Name is required.";
            ErrorTextBlock.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
            return;
        }

        if (VendorComboBox.SelectedItem is not Vendor selectedVendor)
        {
            args.Cancel = true;
            ErrorTextBlock.Text = "Vendor is required.";
            ErrorTextBlock.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
            return;
        }

        // Set organization ID for new subscriptions
        if (!_isEditMode)
        {
            var orgService = App.Host!.Services.GetRequiredService<Services.IOrganizationService>();
            Subscription.OrganizationId = orgService.GetCurrentOrganizationId();
        }

        Subscription.Name = NameTextBox.Text.Trim();
        Subscription.VendorId = selectedVendor.Id;
        Subscription.Plan = string.IsNullOrWhiteSpace(PlanTextBox.Text) ? null : PlanTextBox.Text.Trim();
        Subscription.Reference = string.IsNullOrWhiteSpace(ReferenceTextBox.Text) ? null : ReferenceTextBox.Text.Trim();
        Subscription.Notes = string.IsNullOrWhiteSpace(NotesTextBox.Text) ? null : NotesTextBox.Text.Trim();
        Subscription.Currency = CurrencyComboBox.SelectedItem is ComboBoxItem currItem
            ? currItem.Tag?.ToString() ?? "INR"
            : "INR";

        if (!double.IsNaN(AmountNumberBox.Value))
        {
            Subscription.Amount = (decimal)AmountNumberBox.Value;
        }

        // Parse billing cycle
        if (BillingCycleComboBox.SelectedItem is ComboBoxItem billingItem)
        {
            var tag = billingItem.Tag?.ToString() ?? "Monthly";
            Subscription.BillingCycle = Enum.TryParse<BillingCycle>(tag, out var cycle) ? cycle : BillingCycle.Monthly;
        }
    }

    private void ContentDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        Subscription = null!;
    }
}
