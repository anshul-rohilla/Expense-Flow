using Microsoft.UI.Xaml.Controls;
using Expense_Flow.Models;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace Expense_Flow.Views.PaymentModes;

public sealed partial class PaymentModeDialog : ContentDialog
{
    public PaymentMode PaymentMode { get; private set; }
    private bool _isEditMode;
    public ObservableCollection<Contact> Contacts { get; } = new();

    public PaymentModeDialog(PaymentMode? paymentMode = null)
    {
        InitializeComponent();
        _isEditMode = paymentMode != null;
        Title = _isEditMode ? "Edit Payment Mode" : "Add Payment Mode";
        PrimaryButtonText = _isEditMode ? "Update" : "Create";
        SecondaryButtonText = "Cancel";

        PaymentMode = paymentMode ?? new PaymentMode();
        
        LoadContactsAsync();

        // Set initial values if in edit mode
        if (_isEditMode && paymentMode != null)
        {
            NameTextBox.Text = paymentMode.Name ?? string.Empty;
            TypeComboBox.SelectedIndex = (int)paymentMode.Type;
            CardTypeTextBox.Text = paymentMode.CardType ?? string.Empty;
            LastFourDigitsTextBox.Text = paymentMode.LastFourDigits ?? string.Empty;
            BalanceTextBox.Text = paymentMode.Balance?.ToString() ?? string.Empty;
            UpiIdTextBox.Text = paymentMode.UpiId ?? string.Empty;
            RequiresSettlementCheckBox.IsChecked = paymentMode.RequiresSettlement;
            
            // Show appropriate fields based on type
            UpdateFieldVisibility();
        }
        else
        {
            // Default to Card type for new payment modes
            TypeComboBox.SelectedIndex = 0;
            // Set default balance to 0 for new cash payment modes
            BalanceTextBox.Text = "0";
            UpdateFieldVisibility();
        }

        // Add text changed event for Last Four Digits to restrict to 4 digits
        LastFourDigitsTextBox.TextChanged += LastFourDigitsTextBox_TextChanged;
        // Add text changed event for Balance to allow only numbers
        BalanceTextBox.TextChanging += BalanceTextBox_TextChanging;
    }

    private async void LoadContactsAsync()
    {
        try
        {
            var contactService = App.Host!.Services.GetRequiredService<Services.IContactService>();
            var orgService = App.Host!.Services.GetRequiredService<Services.IOrganizationService>();
            var orgId = orgService.GetCurrentOrganizationId();
            var result = await contactService.GetAllContactsAsync(orgId);
            
            if (result.Success && result.Data != null)
            {
                Contacts.Clear();
                foreach (var contact in result.Data)
                {
                    Contacts.Add(contact);
                }

                if (_isEditMode && PaymentMode.ContactId.HasValue)
                {
                    ContactComboBox.SelectedItem = Contacts.FirstOrDefault(c => c.Id == PaymentMode.ContactId.Value);
                }
                else if (Contacts.Count > 0)
                {
                    ContactComboBox.SelectedIndex = 0;
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading contacts: {ex.Message}");
        }
    }

    private void TypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdateFieldVisibility();
    }

    private void UpdateFieldVisibility()
    {
        if (TypeComboBox.SelectedIndex < 0) return;

        var selectedType = (PaymentModeType)TypeComboBox.SelectedIndex;

        // Hide all type-specific fields first
        CardFieldsPanel.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
        CashFieldsPanel.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
        UpiFieldsPanel.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;

        // Show fields based on selected type
        switch (selectedType)
        {
            case PaymentModeType.Card:
                CardFieldsPanel.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
                break;
            case PaymentModeType.Cash:
                CashFieldsPanel.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
                break;
            case PaymentModeType.UPI:
                UpiFieldsPanel.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
                break;
        }
    }

    private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        // Reset error message
        ErrorTextBlock.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;

        // Validate Name
        if (string.IsNullOrWhiteSpace(NameTextBox.Text))
        {
            args.Cancel = true;
            ErrorTextBlock.Text = "Name is required.";
            ErrorTextBlock.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
            return;
        }

        // Validate Type
        if (TypeComboBox.SelectedIndex < 0)
        {
            args.Cancel = true;
            ErrorTextBlock.Text = "Type is required.";
            ErrorTextBlock.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
            return;
        }

        // Validate Contact (mandatory for all types)
        if (ContactComboBox.SelectedItem is not Contact contact || contact.Id <= 0)
        {
            args.Cancel = true;
            ErrorTextBlock.Text = "Contact is required.";
            ErrorTextBlock.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
            return;
        }

        var selectedType = (PaymentModeType)TypeComboBox.SelectedIndex;

        // Type-specific validation
        switch (selectedType)
        {
            case PaymentModeType.Card:
                // Validate Card Type
                if (string.IsNullOrWhiteSpace(CardTypeTextBox.Text))
                {
                    args.Cancel = true;
                    ErrorTextBlock.Text = "Card Type is required for Card payment mode.";
                    ErrorTextBlock.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
                    return;
                }

                // Validate Last Four Digits
                if (string.IsNullOrWhiteSpace(LastFourDigitsTextBox.Text))
                {
                    args.Cancel = true;
                    ErrorTextBlock.Text = "Last Four Digits is required for Card payment mode.";
                    ErrorTextBlock.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
                    return;
                }

                if (LastFourDigitsTextBox.Text.Length != 4 || !LastFourDigitsTextBox.Text.All(char.IsDigit))
                {
                    args.Cancel = true;
                    ErrorTextBlock.Text = "Last Four Digits must be exactly 4 digits.";
                    ErrorTextBlock.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
                    return;
                }
                break;

            case PaymentModeType.Cash:
                // Validate Balance
                if (string.IsNullOrWhiteSpace(BalanceTextBox.Text))
                {
                    args.Cancel = true;
                    ErrorTextBlock.Text = "Balance is required for Cash payment mode.";
                    ErrorTextBlock.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
                    return;
                }

                if (!decimal.TryParse(BalanceTextBox.Text, out var balance) || balance < 0)
                {
                    args.Cancel = true;
                    ErrorTextBlock.Text = "Please enter a valid balance amount.";
                    ErrorTextBlock.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
                    return;
                }
                break;

            case PaymentModeType.UPI:
                // Validate UPI ID
                if (string.IsNullOrWhiteSpace(UpiIdTextBox.Text))
                {
                    args.Cancel = true;
                    ErrorTextBlock.Text = "UPI ID is required for UPI payment mode.";
                    ErrorTextBlock.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
                    return;
                }

                if (!UpiIdTextBox.Text.Contains('@'))
                {
                    args.Cancel = true;
                    ErrorTextBlock.Text = "Please enter a valid UPI ID (e.g., username@bank).";
                    ErrorTextBlock.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
                    return;
                }
                break;
        }

        // All validations passed, set the properties
        if (!_isEditMode)
        {
            var orgService = App.Host!.Services.GetRequiredService<Services.IOrganizationService>();
            PaymentMode.OrganizationId = orgService.GetCurrentOrganizationId();
        }

        PaymentMode.Name = NameTextBox.Text.Trim();
        PaymentMode.Type = selectedType;
        PaymentMode.ContactId = contact.Id;
        PaymentMode.RequiresSettlement = RequiresSettlementCheckBox.IsChecked == true;

        // Set type-specific properties
        switch (selectedType)
        {
            case PaymentModeType.Card:
                PaymentMode.CardType = CardTypeTextBox.Text.Trim();
                PaymentMode.LastFourDigits = LastFourDigitsTextBox.Text.Trim();
                PaymentMode.Balance = null;
                PaymentMode.UpiId = null;
                break;

            case PaymentModeType.Cash:
                if (decimal.TryParse(BalanceTextBox.Text, out var cashBalance))
                {
                    PaymentMode.Balance = cashBalance;
                }
                PaymentMode.CardType = null;
                PaymentMode.LastFourDigits = null;
                PaymentMode.UpiId = null;
                break;

            case PaymentModeType.UPI:
                PaymentMode.UpiId = UpiIdTextBox.Text.Trim();
                PaymentMode.CardType = null;
                PaymentMode.LastFourDigits = null;
                PaymentMode.Balance = null;
                break;
        }
    }

    private void ContentDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        PaymentMode = null!;
    }

    private void LastFourDigitsTextBox_TextChanged(object sender, Microsoft.UI.Xaml.Controls.TextChangedEventArgs e)
    {
        // Restrict to 4 digits only
        if (LastFourDigitsTextBox.Text.Length > 4)
        {
            LastFourDigitsTextBox.Text = LastFourDigitsTextBox.Text.Substring(0, 4);
            LastFourDigitsTextBox.SelectionStart = 4;
        }
    }

    private void BalanceTextBox_TextChanging(TextBox sender, TextBoxTextChangingEventArgs args)
    {
        // Allow only numbers and decimal point
        if (!string.IsNullOrEmpty(sender.Text))
        {
            if (!System.Text.RegularExpressions.Regex.IsMatch(sender.Text, @"^[0-9]*\.?[0-9]*$"))
            {
                var cursorPosition = sender.SelectionStart - 1;
                sender.Text = sender.Text.Remove(cursorPosition, 1);
                sender.SelectionStart = cursorPosition;
            }
        }
    }
}
