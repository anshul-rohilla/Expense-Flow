using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Expense_Flow.Models;
using Expense_Flow.Services;

namespace Expense_Flow.ViewModels;

public partial class DashboardViewModel : ViewModelBase
{
    private readonly IExpenseService _expenseService;
    private readonly IProjectService _projectService;

    [ObservableProperty]
    private decimal _totalExpenses;

    [ObservableProperty]
    private int _activeProjectsCount;

    [ObservableProperty]
    private decimal _thisMonthExpenses;

    [ObservableProperty]
    private decimal _budgetUsedPercentage;

    [ObservableProperty]
    private ObservableCollection<Expense> _recentExpenses = new();

    [ObservableProperty]
    private ObservableCollection<Project> _topProjects = new();

    public DashboardViewModel(IExpenseService expenseService, IProjectService projectService)
    {
        _expenseService = expenseService;
        _projectService = projectService;
    }

    [RelayCommand]
    private async Task LoadDashboardDataAsync()
    {
        await ExecuteAsync(async () =>
        {
            await Task.WhenAll(
                LoadTotalExpensesAsync(),
                LoadActiveProjectsCountAsync(),
                LoadThisMonthExpensesAsync(),
                LoadRecentExpensesAsync(),
                LoadTopProjectsAsync()
            );
        }, "Loading dashboard...");
    }

    private async Task LoadTotalExpensesAsync()
    {
        var result = await _expenseService.GetTotalExpensesAsync();
        if (result.Success)
        {
            TotalExpenses = result.Data;
        }
    }

    private async Task LoadActiveProjectsCountAsync()
    {
        var result = await _projectService.GetAllProjectsAsync(includeArchived: false);
        if (result.Success && result.Data != null)
        {
            ActiveProjectsCount = result.Data.Count();
        }
    }

    private async Task LoadThisMonthExpensesAsync()
    {
        var startOfMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
        var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);

        var result = await _expenseService.GetTotalExpensesAsync(
            startDate: startOfMonth,
            endDate: endOfMonth);

        if (result.Success)
        {
            ThisMonthExpenses = result.Data;
        }
    }

    private async Task LoadRecentExpensesAsync()
    {
        var result = await _expenseService.GetAllExpensesAsync();
        if (result.Success && result.Data != null)
        {
            RecentExpenses = new ObservableCollection<Expense>(
                result.Data.OrderByDescending(e => e.CreatedAt).Take(5));
        }
    }

    private async Task LoadTopProjectsAsync()
    {
        var result = await _projectService.GetAllProjectsAsync(includeArchived: false);
        if (result.Success && result.Data != null)
        {
            TopProjects = new ObservableCollection<Project>(
                result.Data.OrderBy(p => p.Name).Take(5));
        }
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        await LoadDashboardDataAsync();
    }
}
