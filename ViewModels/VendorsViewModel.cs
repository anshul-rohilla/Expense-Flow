using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Expense_Flow.Models;
using Expense_Flow.Services;

namespace Expense_Flow.ViewModels;

public partial class VendorsViewModel : ViewModelBase
{
    private readonly IVendorService _vendorService;
    private readonly IOrganizationService _organizationService;

    [ObservableProperty]
    private ObservableCollection<Vendor> _vendors = new();

    [ObservableProperty]
    private ObservableCollection<Vendor> _filteredVendors = new();

    [ObservableProperty]
    private Vendor? _selectedVendor;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private bool _showArchived;

    [ObservableProperty]
    private int _activeVendorCount;

    [ObservableProperty]
    private int _totalVendorCount;

    public VendorsViewModel(IVendorService vendorService, IOrganizationService organizationService)
    {
        _vendorService = vendorService;
        _organizationService = organizationService;
    }

    [RelayCommand]
    private async Task LoadVendorsAsync()
    {
        await ExecuteAsync(async () =>
        {
            var orgId = _organizationService.GetCurrentOrganizationId();
            var result = await _vendorService.GetAllVendorsAsync(orgId);
            if (result.Success && result.Data != null)
            {
                Vendors = new ObservableCollection<Vendor>(result.Data);
                TotalVendorCount = Vendors.Count;
                ActiveVendorCount = Vendors.Count(v => !v.IsArchived);
                FilterVendors();
            }
            else
            {
                SetError(result.GetErrorMessage());
            }
        }, "Loading vendors...");
    }

    [RelayCommand]
    private async Task DeleteVendorAsync(Vendor vendor)
    {
        if (vendor == null) return;

        await ExecuteAsync(async () =>
        {
            var result = await _vendorService.DeleteVendorAsync(vendor.Id);
            if (result.Success)
            {
                Vendors.Remove(vendor);
                FilterVendors();
            }
            else
            {
                SetError(result.GetErrorMessage());
            }
        }, "Deleting vendor...");
    }

    [RelayCommand]
    private async Task ArchiveVendorAsync(Vendor vendor)
    {
        if (vendor == null) return;

        await ExecuteAsync(async () =>
        {
            vendor.IsArchived = true;
            var result = await _vendorService.UpdateVendorAsync(vendor);
            if (result.Success)
            {
                await LoadVendorsAsync();
            }
            else
            {
                SetError(result.GetErrorMessage());
            }
        }, "Archiving vendor...");
    }

    [RelayCommand]
    private async Task UnarchiveVendorAsync(Vendor vendor)
    {
        if (vendor == null) return;

        await ExecuteAsync(async () =>
        {
            vendor.IsArchived = false;
            var result = await _vendorService.UpdateVendorAsync(vendor);
            if (result.Success)
            {
                await LoadVendorsAsync();
            }
            else
            {
                SetError(result.GetErrorMessage());
            }
        }, "Unarchiving vendor...");
    }

    private void FilterVendors()
    {
        var filtered = Vendors.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            filtered = filtered.Where(v =>
                v.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                (v.Notes?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (v.Website?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        FilteredVendors = new ObservableCollection<Vendor>(filtered);
    }

    partial void OnSearchTextChanged(string value)
    {
        FilterVendors();
    }

    partial void OnShowArchivedChanged(bool value)
    {
        _ = LoadVendorsAsync();
    }
}
