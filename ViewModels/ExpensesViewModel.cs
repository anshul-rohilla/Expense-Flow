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

    [ObservableProperty]
    private ObservableCollection<Expense> _expenses = new();

    [ObservableProperty]
    private ObservableCollection<Project> _projects = new();

    [ObservableProperty]
    private Expense? _selectedExpense;

    [ObservableProperty]
    private Project? _selectedProject;

    [ObservableProperty]
    private DateTime? _startDate;

    [ObservableProperty]
    private DateTime? _endDate;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private decimal _totalAmount;

    public ExpensesViewModel(IExpenseService expenseService, IProjectService projectService)
    {
        _expenseService = expenseService;
        _projectService = projectService;
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
                Expenses = new ObservableCollection<Expense>(result.Data);
                await CalculateTotalAsync();
            }
            else
            {
                SetError(result.GetErrorMessage());
            }
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

    [RelayCommand]
    private async Task DeleteExpenseAsync(Expense expense)
    {
        if (expense == null) return;

        await ExecuteAsync(async () =>
        {
            var result = await _expenseService.DeleteExpenseAsync(expense.Id);
            if (result.Success)
            {
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
    private async Task FilterByProjectAsync()
    {
        if (SelectedProject == null) return;

        await ExecuteAsync(async () =>
        {
            var result = await _expenseService.GetExpensesByProjectAsync(SelectedProject.Id);
            if (result.Success && result.Data != null)
            {
                Expenses = new ObservableCollection<Expense>(result.Data);
                await CalculateTotalAsync();
            }
            else
            {
                SetError(result.GetErrorMessage());
            }
        }, "Filtering expenses...");
    }

    [RelayCommand]
    private async Task ClearFiltersAsync()
    {
        SelectedProject = null;
        StartDate = null;
        EndDate = null;
        SearchText = string.Empty;
        await LoadExpensesAsync();
    }

    private async Task CalculateTotalAsync()
    {
        var result = await _expenseService.GetTotalExpensesAsync(
            SelectedProject?.Id,
            StartDate,
            EndDate);

        if (result.Success)
        {
            TotalAmount = result.Data;
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
}
