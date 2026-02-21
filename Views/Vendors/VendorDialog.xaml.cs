using Microsoft.UI.Xaml.Controls;
using Expense_Flow.Models;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace Expense_Flow.Views.Vendors;

public sealed partial class VendorDialog : ContentDialog
{
    public Vendor Vendor { get; private set; }
    private bool _isEditMode;
    public ObservableCollection<Contact> Contacts { get; } = new();

    public VendorDialog(Vendor? vendor = null)
    {
        InitializeComponent();
        _isEditMode = vendor != null;
        Title = _isEditMode ? "Edit Vendor" : "Add Vendor";
        PrimaryButtonText = _isEditMode ? "Update" : "Create";
        SecondaryButtonText = "Cancel";

        Vendor = vendor ?? new Vendor();

        LoadContactsAsync();

        if (_isEditMode && vendor != null)
        {
            NameTextBox.Text = vendor.Name ?? string.Empty;
            WebsiteTextBox.Text = vendor.Website ?? string.Empty;
            AccountReferenceTextBox.Text = vendor.AccountReference ?? string.Empty;
            NotesTextBox.Text = vendor.Notes ?? string.Empty;
        }
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
                Contacts.Add(new Contact { Id = 0, Name = "(None)" });
                foreach (var contact in result.Data)
                {
                    Contacts.Add(contact);
                }

                if (_isEditMode && Vendor.ContactId.HasValue)
                {
                    ContactComboBox.SelectedItem = Contacts.FirstOrDefault(c => c.Id == Vendor.ContactId.Value);
                }
                else
                {
                    ContactComboBox.SelectedIndex = 0;
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading contacts: {ex.Message}");
            if (Contacts.Count == 0)
            {
                Contacts.Add(new Contact { Id = 0, Name = "(None)" });
                ContactComboBox.SelectedIndex = 0;
            }
        }
    }

    private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        ErrorTextBlock.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;

        if (string.IsNullOrWhiteSpace(NameTextBox.Text))
        {
            args.Cancel = true;
            ErrorTextBlock.Text = "Vendor name is required.";
            ErrorTextBlock.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
            return;
        }

        // Set OrganizationId for new vendors
        if (!_isEditMode)
        {
            var orgService = App.Host!.Services.GetRequiredService<Services.IOrganizationService>();
            Vendor.OrganizationId = orgService.GetCurrentOrganizationId();
        }

        Vendor.Name = NameTextBox.Text.Trim();
        Vendor.Website = string.IsNullOrWhiteSpace(WebsiteTextBox.Text) ? null : WebsiteTextBox.Text.Trim();
        Vendor.AccountReference = string.IsNullOrWhiteSpace(AccountReferenceTextBox.Text) ? null : AccountReferenceTextBox.Text.Trim();
        Vendor.Notes = string.IsNullOrWhiteSpace(NotesTextBox.Text) ? null : NotesTextBox.Text.Trim();

        if (ContactComboBox.SelectedItem is Contact contact && contact.Id > 0)
        {
            Vendor.ContactId = contact.Id;
        }
        else
        {
            Vendor.ContactId = null;
        }
    }

    private void ContentDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        Vendor = null!;
    }
}
