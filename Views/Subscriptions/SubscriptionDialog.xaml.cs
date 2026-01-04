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
    public ObservableCollection<Contact> Contacts { get; } = new();
    public ObservableCollection<ExpenseType> ExpenseTypes { get; } = new();

    public SubscriptionDialog(Subscription? subscription = null)
    {
        InitializeComponent();
        _isEditMode = subscription != null;
        Title = _isEditMode ? "Edit Subscription" : "Add Subscription";
        PrimaryButtonText = _isEditMode ? "Update" : "Create";
        SecondaryButtonText = "Cancel";

        // Initialize the Subscription property
        Subscription = subscription ?? new Subscription();
        
        LoadContactsAsync();
        LoadExpenseTypesAsync();

        // Set initial values if in edit mode
        if (_isEditMode && subscription != null)
        {
            NameTextBox.Text = subscription.Name ?? string.Empty;
            ReferenceTextBox.Text = subscription.Reference ?? string.Empty;
            
            // Set radio button based on IsVendor flag
            if (subscription.IsVendor)
            {
                VendorRadioButton.IsChecked = true;
            }
            else
            {
                SubscriptionRadioButton.IsChecked = true;
            }
        }

        // Update title based on radio button selection
        SubscriptionRadioButton.Checked += (s, e) => UpdateDialogTitle();
        VendorRadioButton.Checked += (s, e) => UpdateDialogTitle();
    }

    private void UpdateDialogTitle()
    {
        if (VendorRadioButton.IsChecked == true)
        {
            Title = _isEditMode ? "Edit Vendor" : "Add Vendor";
        }
        else
        {
            Title = _isEditMode ? "Edit Subscription" : "Add Subscription";
        }
    }

    private async void LoadContactsAsync()
    {
        try
        {
            var contactService = App.Host!.Services.GetRequiredService<Services.IContactService>();
            var result = await contactService.GetAllContactsAsync();
            
            if (result.Success && result.Data != null)
            {
                Contacts.Clear();
                Contacts.Add(new Contact { Id = 0, Name = "(None)" });
                foreach (var contact in result.Data)
                {
                    Contacts.Add(contact);
                }

                if (_isEditMode && Subscription.ContactId.HasValue)
                {
                    ContactComboBox.SelectedItem = Contacts.FirstOrDefault(c => c.Id == Subscription.ContactId.Value);
                }
                else
                {
                    ContactComboBox.SelectedIndex = 0;
                }
            }
        }
        catch (Exception ex)
        {
            // Handle error silently or show error message
            System.Diagnostics.Debug.WriteLine($"Error loading contacts: {ex.Message}");
            // Ensure at least the "(None)" option is available
            if (Contacts.Count == 0)
            {
                Contacts.Add(new Contact { Id = 0, Name = "(None)" });
                ContactComboBox.SelectedIndex = 0;
            }
        }
    }

    private async void LoadExpenseTypesAsync()
    {
        try
        {
            var expenseTypeService = App.Host!.Services.GetRequiredService<Services.IExpenseTypeService>();
            var result = await expenseTypeService.GetAllExpenseTypesAsync();
            
            if (result.Success && result.Data != null)
            {
                ExpenseTypes.Clear();
                foreach (var type in result.Data)
                {
                    ExpenseTypes.Add(type);
                }

                if (_isEditMode && !string.IsNullOrEmpty(Subscription.Type))
                {
                    var matchingType = ExpenseTypes.FirstOrDefault(et => et.Name == Subscription.Type);
                    if (matchingType != null)
                    {
                        TypeComboBox.SelectedItem = matchingType;
                    }
                }
                else if (ExpenseTypes.Count > 0)
                {
                    TypeComboBox.SelectedIndex = 0;
                }
            }
        }
        catch (Exception ex)
        {
            // Handle error silently or show error message
            System.Diagnostics.Debug.WriteLine($"Error loading expense types: {ex.Message}");
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

        // Validate Type (mandatory)
        if (TypeComboBox.SelectedItem is not ExpenseType selectedType)
        {
            args.Cancel = true;
            ErrorTextBlock.Text = "Type is required.";
            ErrorTextBlock.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
            return;
        }

        // Validate Contact (mandatory)
        if (ContactComboBox.SelectedItem is not Contact contact || contact.Id <= 0)
        {
            args.Cancel = true;
            ErrorTextBlock.Text = "Contact is required.";
            ErrorTextBlock.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
            return;
        }

        // All validations passed
        Subscription.Name = NameTextBox.Text.Trim();
        Subscription.Type = selectedType.Name;
        Subscription.Reference = string.IsNullOrWhiteSpace(ReferenceTextBox.Text) ? null : ReferenceTextBox.Text.Trim();
        Subscription.ContactId = contact.Id;
        Subscription.IsVendor = VendorRadioButton.IsChecked == true;
    }

    private void ContentDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        Subscription = null!;
    }
}
