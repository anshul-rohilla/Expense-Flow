using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Expense_Flow.Models;
using Expense_Flow.Services;

namespace Expense_Flow.ViewModels;

public partial class ExpensesViewModel : ViewModelBase
{
    private readonly IExpenseService _expenseService;
    private readonly IProjectService _projectService;
    private readonly IExpenseTypeService? _expenseTypeService;
    private readonly IPaymentModeService? _paymentModeService;

    [ObservableProperty]
    private ObservableCollection<Expense> _expenses = new();

    [ObservableProperty]
    private ObservableCollection<Project> _projects = new();

    [ObservableProperty]
    private ObservableCollection<ExpenseType> _expenseTypes = new();

    [ObservableProperty]
    private ObservableCollection<PaymentMode> _paymentModes = new();

    [ObservableProperty]
    private Expense? _selectedExpense;

    [ObservableProperty]
    private Project? _selectedProject;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Expenses))]
    private ExpenseType? _selectedExpenseType;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Expenses))]
    private PaymentMode? _selectedPaymentMode;

    [ObservableProperty]
    private DateTime? _startDate;

    [ObservableProperty]
    private DateTime? _endDate;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private string _totalAmount = "$0.00";

    private ObservableCollection<Expense> _allExpenses = new();

    public ExpensesViewModel(
        IExpenseService expenseService, 
        IProjectService projectService,
        IExpenseTypeService? expenseTypeService = null,
        IPaymentModeService? paymentModeService = null)
    {
        _expenseService = expenseService;
        _projectService = projectService;
        _expenseTypeService = expenseTypeService;
        _paymentModeService = paymentModeService;
    }

    [RelayCommand]
    private async Task LoadExpensesAsync()
    {
        await ExecuteAsync(async () =>
        {
            var result = StartDate.HasValue || EndDate.HasValue
                ? await _expenseService.GetExpensesByDateRangeAsync(StartDate, EndDate)
                : await _expenseService.GetAllExpensesAsync();

            if (result.Success && result.Data != null)
            {
                _allExpenses = new ObservableCollection<Expense>(result.Data);
                ApplyFilters();
                await CalculateTotalAsync();
            }
            else
            {
                SetError(result.GetErrorMessage());
            }

            // Load filter options
            if (_expenseTypeService != null)
                await LoadExpenseTypesAsync();
            if (_paymentModeService != null)
                await LoadPaymentModesAsync();
        }, "Loading expenses...");
    }

    [RelayCommand]
    private async Task LoadProjectsAsync()
    {
        await ExecuteAsync(async () =>
        {
            var result = await _projectService.GetAllProjectsAsync();
            if (result.Success && result.Data != null)
            {
                Projects = new ObservableCollection<Project>(result.Data);
            }
            else
            {
                SetError(result.GetErrorMessage());
            }
        }, "Loading projects...");
    }

    private async Task LoadExpenseTypesAsync()
    {
        if (_expenseTypeService == null) return;
        
        var result = await _expenseTypeService.GetAllExpenseTypesAsync();
        if (result.Success && result.Data != null)
        {
            ExpenseTypes = new ObservableCollection<ExpenseType>(result.Data);
        }
    }

    private async Task LoadPaymentModesAsync()
    {
        if (_paymentModeService == null) return;
        
        var result = await _paymentModeService.GetAllPaymentModesAsync();
        if (result.Success && result.Data != null)
        {
            PaymentModes = new ObservableCollection<PaymentMode>(result.Data);
        }
    }

    [RelayCommand]
    private async Task DeleteExpenseAsync(Expense expense)
    {
        if (expense == null) return;

        await ExecuteAsync(async () =>
        {
            var result = await _expenseService.DeleteExpenseAsync(expense.Id);
            if (result.Success)
            {
                _allExpenses.Remove(expense);
                Expenses.Remove(expense);
                await CalculateTotalAsync();
            }
            else
            {
                SetError(result.GetErrorMessage());
            }
        }, "Deleting expense...");
    }

    [RelayCommand]
    private async Task ClearFiltersAsync()
    {
        SelectedProject = null;
        SelectedExpenseType = null;
        SelectedPaymentMode = null;
        StartDate = null;
        EndDate = null;
        SearchText = string.Empty;
        ApplyFilters();
        await CalculateTotalAsync();
    }

    private void ApplyFilters()
    {
        var filtered = _allExpenses.AsEnumerable();

        if (SelectedProject != null)
        {
            filtered = filtered.Where(e => e.ProjectId == SelectedProject.Id);
        }

        if (SelectedExpenseType != null)
        {
            filtered = filtered.Where(e => e.ExpenseTypeId == SelectedExpenseType.Id);
        }

        if (SelectedPaymentMode != null)
        {
            filtered = filtered.Where(e => e.PaymentModeId == SelectedPaymentMode.Id);
        }

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            filtered = filtered.Where(e => 
                e.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                (e.Description?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        Expenses = new ObservableCollection<Expense>(filtered);
    }

    private async Task CalculateTotalAsync()
    {
        try
        {
            var settingsService = App.Host?.Services?.GetService(typeof(ISettingsService)) as ISettingsService;
            var total = Expenses.Sum(e => e.Amount);
            
            if (settingsService != null)
            {
                TotalAmount = settingsService.FormatCurrency(total);
            }
            else
            {
                TotalAmount = $"${total:N2}";
            }
        }
        catch
        {
            var total = Expenses.Sum(e => e.Amount);
            TotalAmount = $"${total:N2}";
        }
    }

    partial void OnStartDateChanged(DateTime? value)
    {
        _ = LoadExpensesAsync();
    }

    partial void OnEndDateChanged(DateTime? value)
    {
        _ = LoadExpensesAsync();
    }

    partial void OnSelectedProjectChanged(Project? value)
    {
        ApplyFilters();
        _ = CalculateTotalAsync();
    }

    partial void OnSelectedExpenseTypeChanged(ExpenseType? value)
    {
        ApplyFilters();
        _ = CalculateTotalAsync();
    }

    partial void OnSelectedPaymentModeChanged(PaymentMode? value)
    {
        ApplyFilters();
        _ = CalculateTotalAsync();
    }
}
