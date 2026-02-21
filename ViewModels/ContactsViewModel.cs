using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Expense_Flow.Models;
using Expense_Flow.Services;

namespace Expense_Flow.ViewModels;

public partial class ContactsViewModel : ViewModelBase
{
    private readonly IContactService _contactService;
    private readonly IOrganizationService _organizationService;

    [ObservableProperty]
    private ObservableCollection<Contact> _contacts = new();

    [ObservableProperty]
    private Contact? _selectedContact;

    [ObservableProperty]
    private string _searchText = string.Empty;

    public ContactsViewModel(IContactService contactService, IOrganizationService organizationService)
    {
        _contactService = contactService;
        _organizationService = organizationService;
    }

    [RelayCommand]
    private async Task LoadContactsAsync()
    {
        await ExecuteAsync(async () =>
        {
            var orgId = _organizationService.GetCurrentOrganizationId();
            var result = await _contactService.GetAllContactsAsync(orgId);
            if (result.Success && result.Data != null)
            {
                Contacts = new ObservableCollection<Contact>(result.Data);
            }
            else
            {
                SetError(result.GetErrorMessage());
            }
        }, "Loading contacts...");
    }

    [RelayCommand]
    private async Task DeleteContactAsync(Contact contact)
    {
        if (contact == null) return;

        await ExecuteAsync(async () =>
        {
            var result = await _contactService.DeleteContactAsync(contact.Id);
            if (result.Success)
            {
                Contacts.Remove(contact);
            }
            else
            {
                SetError(result.GetErrorMessage());
            }
        }, "Deleting contact...");
    }

    [RelayCommand]
    private void SearchContacts()
    {
        // Will be implemented with filtering
    }

    partial void OnSearchTextChanged(string value)
    {
        // Real-time search filtering will be added
    }
}
