using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Expense_Flow.Models;
using Expense_Flow.Services;

namespace Expense_Flow.ViewModels;

public partial class PaymentModesViewModel : ViewModelBase
{
    private readonly IPaymentModeService _paymentModeService;
    private readonly ISettingsService _settingsService;
    private readonly IOrganizationService _organizationService;

    [ObservableProperty]
    private ObservableCollection<PaymentMode> _allPaymentModes = new();

    [ObservableProperty]
    private ObservableCollection<PaymentMode> _filteredPaymentModes = new();

    [ObservableProperty]
    private PaymentMode? _selectedPaymentMode;

    [ObservableProperty]
    private PaymentModeType? _selectedType;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private string _currencySymbol = "$";

    public PaymentModesViewModel(IPaymentModeService paymentModeService, ISettingsService settingsService, IOrganizationService organizationService)
    {
        _paymentModeService = paymentModeService;
        _settingsService = settingsService;
        _organizationService = organizationService;
        _currencySymbol = _settingsService.GetCurrencySymbol();
    }

    [RelayCommand]
    private async Task LoadPaymentModesAsync()
    {
        await ExecuteAsync(async () =>
        {
            // Refresh currency symbol in case it changed
            CurrencySymbol = _settingsService.GetCurrencySymbol();
            
            var orgId = _organizationService.GetCurrentOrganizationId();
            var result = await _paymentModeService.GetAllPaymentModesAsync(orgId);
            if (result.Success && result.Data != null)
            {
                AllPaymentModes = new ObservableCollection<PaymentMode>(result.Data);
                FilterPaymentModes();
            }
            else
            {
                SetError(result.GetErrorMessage());
            }
        }, "Loading payment modes...");
    }

    [RelayCommand]
    private async Task DeletePaymentModeAsync(PaymentMode paymentMode)
    {
        if (paymentMode == null) return;

        await ExecuteAsync(async () =>
        {
            var result = await _paymentModeService.DeletePaymentModeAsync(paymentMode.Id);
            if (result.Success)
            {
                AllPaymentModes.Remove(paymentMode);
                FilterPaymentModes();
            }
            else
            {
                SetError(result.GetErrorMessage());
            }
        }, "Deleting payment mode...");
    }

    [RelayCommand]
    private void FilterByType(PaymentModeType? type)
    {
        SelectedType = type;
        FilterPaymentModes();
    }

    public string FormatBalance(decimal? balance)
    {
        if (!balance.HasValue) return $"{CurrencySymbol}0.00";
        return $"{CurrencySymbol}{balance.Value:N2}";
    }

    private void FilterPaymentModes()
    {
        var filtered = AllPaymentModes.AsEnumerable();

        if (SelectedType.HasValue)
        {
            filtered = filtered.Where(pm => pm.Type == SelectedType.Value);
        }

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            filtered = filtered.Where(pm => 
                pm.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase));
        }

        FilteredPaymentModes = new ObservableCollection<PaymentMode>(filtered);
    }

    partial void OnSearchTextChanged(string value)
    {
        FilterPaymentModes();
    }
}
