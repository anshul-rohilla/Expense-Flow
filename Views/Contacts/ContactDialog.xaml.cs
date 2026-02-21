using Microsoft.UI.Xaml.Controls;
using Expense_Flow.Models;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Text.RegularExpressions;

namespace Expense_Flow.Views.Contacts;

public sealed partial class ContactDialog : ContentDialog
{
    public Contact Contact { get; private set; }
    private bool _isEditMode;

    public ContactDialog(Contact? contact = null)
    {
        InitializeComponent();
        _isEditMode = contact != null;
        Title = _isEditMode ? "Edit Contact" : "Add Contact";
        PrimaryButtonText = _isEditMode ? "Update" : "Create";
        SecondaryButtonText = "Cancel";

        Contact = contact ?? new Contact();

        // Set initial values if in edit mode
        if (_isEditMode && contact != null)
        {
            NameTextBox.Text = contact.Name ?? string.Empty;
            ReferenceTextBox.Text = contact.Reference ?? string.Empty;
            PhoneTextBox.Text = contact.Phone ?? string.Empty;
            EmailTextBox.Text = contact.Email ?? string.Empty;
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

        // Validate Email (mandatory)
        if (string.IsNullOrWhiteSpace(EmailTextBox.Text))
        {
            args.Cancel = true;
            ErrorTextBlock.Text = "Email is required.";
            ErrorTextBlock.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
            return;
        }

        if (!IsValidEmail(EmailTextBox.Text.Trim()))
        {
            args.Cancel = true;
            ErrorTextBlock.Text = "Please enter a valid email address.";
            ErrorTextBlock.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
            return;
        }

        // Validate Phone (mandatory)
        if (string.IsNullOrWhiteSpace(PhoneTextBox.Text))
        {
            args.Cancel = true;
            ErrorTextBlock.Text = "Phone number is required.";
            ErrorTextBlock.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
            return;
        }

        if (!IsValidPhone(PhoneTextBox.Text.Trim()))
        {
            args.Cancel = true;
            ErrorTextBlock.Text = "Please enter a valid phone number (10-15 digits).";
            ErrorTextBlock.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
            return;
        }

        // All validations passed, update contact
        if (!_isEditMode)
        {
            var orgService = App.Host!.Services.GetRequiredService<Services.IOrganizationService>();
            Contact.OrganizationId = orgService.GetCurrentOrganizationId();
        }

        Contact.Name = NameTextBox.Text.Trim();
        Contact.Reference = string.IsNullOrWhiteSpace(ReferenceTextBox.Text) ? null : ReferenceTextBox.Text.Trim();
        Contact.Phone = PhoneTextBox.Text.Trim();
        Contact.Email = EmailTextBox.Text.Trim();
    }

    private void ContentDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        Contact = null!;
    }

    private static bool IsValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        try
        {
            // Simple email validation pattern
            var emailPattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
            return Regex.IsMatch(email, emailPattern, RegexOptions.IgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    private static bool IsValidPhone(string phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
            return false;

        // Remove common phone number characters (spaces, dashes, parentheses, plus)
        var digitsOnly = Regex.Replace(phone, @"[\s\-\(\)\+]", "");

        // Check if it contains only digits and is between 10-15 characters
        return Regex.IsMatch(digitsOnly, @"^\d{10,15}$");
    }
}
