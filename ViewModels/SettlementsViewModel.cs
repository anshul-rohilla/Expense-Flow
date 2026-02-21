using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Expense_Flow.Models;
using Expense_Flow.Services;

namespace Expense_Flow.ViewModels;

public partial class SettlementsViewModel : ViewModelBase
{
    private readonly ISettlementService _settlementService;
    private readonly IExpenseService _expenseService;
    private readonly IOrganizationService _organizationService;
    private readonly ISettingsService _settingsService;

    [ObservableProperty]
    private ObservableCollection<Settlement> _settlements = new();

    [ObservableProperty]
    private ObservableCollection<Expense> _unsettledExpenses = new();

    [ObservableProperty]
    private Settlement? _selectedSettlement;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private string _totalPendingAmount = string.Empty;

    [ObservableProperty]
    private int _pendingExpenseCount;

    [ObservableProperty]
    private int _selectedTabIndex;

    public SettlementsViewModel(
        ISettlementService settlementService,
        IExpenseService expenseService,
        IOrganizationService organizationService,
        ISettingsService settingsService)
    {
        _settlementService = settlementService;
        _expenseService = expenseService;
        _organizationService = organizationService;
        _settingsService = settingsService;
    }

    [RelayCommand]
    private async Task LoadSettlementsAsync()
    {
        await ExecuteAsync(async () =>
        {
            var orgId = _organizationService.GetCurrentOrganizationId();
            var result = await _settlementService.GetAllSettlementsAsync(orgId);
            if (result.Success && result.Data != null)
            {
                Settlements = new ObservableCollection<Settlement>(
                    result.Data.OrderByDescending(s => s.SettlementDate));
            }
            else
            {
                SetError(result.GetErrorMessage());
            }
        }, "Loading settlements...");
    }

    [RelayCommand]
    private async Task LoadUnsettledExpensesAsync()
    {
        await ExecuteAsync(async () =>
        {
            var orgId = _organizationService.GetCurrentOrganizationId();
            var result = await _settlementService.GetUnsettledExpensesAsync(orgId);
            if (result.Success && result.Data != null)
            {
                UnsettledExpenses = new ObservableCollection<Expense>(result.Data);
                PendingExpenseCount = UnsettledExpenses.Count;

                var totalPending = UnsettledExpenses.Sum(e => e.PendingReimbursement);
                TotalPendingAmount = _settingsService.FormatCurrency(totalPending);
            }
            else
            {
                SetError(result.GetErrorMessage());
            }
        }, "Loading unsettled expenses...");
    }

    [RelayCommand]
    private async Task LoadAllDataAsync()
    {
        await LoadSettlementsAsync();
        await LoadUnsettledExpensesAsync();
    }

    [RelayCommand]
    private async Task CancelSettlementAsync(Settlement settlement)
    {
        if (settlement == null) return;

        await ExecuteAsync(async () =>
        {
            var result = await _settlementService.CancelSettlementAsync(settlement.Id);
            if (result.Success)
            {
                await LoadAllDataAsync();
            }
            else
            {
                SetError(result.GetErrorMessage());
            }
        }, "Cancelling settlement...");
    }

    partial void OnSelectedTabIndexChanged(int value)
    {
        if (value == 0)
            _ = LoadUnsettledExpensesAsync();
        else
            _ = LoadSettlementsAsync();
    }
}
