using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Expense_Flow.Models;
using Expense_Flow.Services;

namespace Expense_Flow.ViewModels;

public enum DateRangeFilter
{
    CurrentMonth,
    PreviousMonth,
    Last30Days,
    Last2Months,
    Last3Months,
    Last1Year
}

public partial class DashboardViewModel : ViewModelBase
{
    private readonly IExpenseService _expenseService;
    private readonly IProjectService _projectService;
    private readonly ISettingsService _settingsService;

    [ObservableProperty]
    private DateRangeFilter _selectedDateRange = DateRangeFilter.Last30Days;

    [ObservableProperty]
    private string _totalExpenses = string.Empty;

    [ObservableProperty]
    private int _activeProjectsCount;

    [ObservableProperty]
    private string _thisMonthExpenses = string.Empty;

    [ObservableProperty]
    private double _budgetUsedPercentage;

    [ObservableProperty]
    private string _budgetRemaining = string.Empty;

    [ObservableProperty]
    private ObservableCollection<Expense> _recentExpenses = new();

    [ObservableProperty]
    private ObservableCollection<Project> _topProjects = new();

    [ObservableProperty]
    private string _totalExpensesChange = "+0% from last month";

    [ObservableProperty]
    private string _newProjectsThisWeek = "0 new this week";

    [ObservableProperty]
    private string _thisMonthTransactions = "0 transactions";

    [ObservableProperty]
    private string _selectedPeriodLabel = "Last 30 Days";

    private decimal _totalMonthlyBudget = 0;
    private decimal _currentMonthTotal = 0;

    public DashboardViewModel(IExpenseService expenseService, IProjectService projectService, ISettingsService settingsService)
    {
        _expenseService = expenseService;
        _projectService = projectService;
        _settingsService = settingsService;

        // Initialize with properly formatted currency from settings
        var zero = _settingsService.FormatCurrency(0);
        _totalExpenses = zero;
        _thisMonthExpenses = zero;
        _budgetRemaining = zero;
    }

    partial void OnSelectedDateRangeChanged(DateRangeFilter value)
    {
        UpdateSelectedPeriodLabel();
        _ = LoadDashboardDataAsync();
    }

    private void UpdateSelectedPeriodLabel()
    {
        SelectedPeriodLabel = SelectedDateRange switch
        {
            DateRangeFilter.CurrentMonth => "Current Month",
            DateRangeFilter.PreviousMonth => "Previous Month",
            DateRangeFilter.Last30Days => "Last 30 Days",
            DateRangeFilter.Last2Months => "Last 2 Months",
            DateRangeFilter.Last3Months => "Last 3 Months",
            DateRangeFilter.Last1Year => "Last 1 Year",
            _ => "Last 30 Days"
        };
    }

    private (DateTime startDate, DateTime endDate) GetDateRangeFromFilter()
    {
        var now = DateTime.Now;
        return SelectedDateRange switch
        {
            DateRangeFilter.CurrentMonth => (new DateTime(now.Year, now.Month, 1), now),
            DateRangeFilter.PreviousMonth => (new DateTime(now.Year, now.Month, 1).AddMonths(-1), new DateTime(now.Year, now.Month, 1).AddDays(-1)),
            DateRangeFilter.Last30Days => (now.AddDays(-30), now),
            DateRangeFilter.Last2Months => (now.AddMonths(-2), now),
            DateRangeFilter.Last3Months => (now.AddMonths(-3), now),
            DateRangeFilter.Last1Year => (now.AddYears(-1), now),
            _ => (now.AddDays(-30), now)
        };
    }

    [RelayCommand]
    private async Task LoadDashboardDataAsync()
    {
        await ExecuteAsync(async () =>
        {
            // Load projects first to calculate budget
            await LoadActiveProjectsCountAsync();
            
            // Then load expenses and other data
            await Task.WhenAll(
                LoadTotalExpensesAsync(),
                LoadThisMonthExpensesAsync(),
                LoadRecentExpensesAsync(),
                LoadTopProjectsAsync()
            );
        }, "Loading dashboard...");
    }

    private async Task LoadTotalExpensesAsync()
    {
        var (startDate, endDate) = GetDateRangeFromFilter();
        var result = await _expenseService.GetTotalExpensesAsync(startDate: startDate, endDate: endDate);
        
        if (result.Success)
        {
            var currentTotal = result.Data;
            TotalExpenses = _settingsService.FormatCurrency(currentTotal);
            
            // Calculate change from previous period of same duration
            TimeSpan duration = endDate - startDate;
            var previousStartDate = startDate.Add(-duration);
            var previousEndDate = startDate.AddDays(-1);
            
            var previousResult = await _expenseService.GetTotalExpensesAsync(startDate: previousStartDate, endDate: previousEndDate);
            
            if (previousResult.Success && previousResult.Data > 0)
            {
                var changePercent = ((currentTotal - previousResult.Data) / previousResult.Data) * 100;
                var sign = changePercent >= 0 ? "+" : "";
                TotalExpensesChange = $"{sign}{changePercent:F1}% from previous period";
            }
            else
            {
                TotalExpensesChange = "No previous data";
            }
        }
    }

    private async Task LoadActiveProjectsCountAsync()
    {
        var result = await _projectService.GetAllProjectsAsync(includeArchived: false);
        if (result.Success && result.Data != null)
        {
            var projects = result.Data.ToList();
            ActiveProjectsCount = projects.Count;
            
            // Calculate total monthly budget from all active projects
            _totalMonthlyBudget = projects.Sum(p => p.MonthlyBudget ?? 0);
            
            // Calculate new projects this week
            var weekAgo = DateTime.Now.AddDays(-7);
            var newProjectsCount = projects.Count(p => p.CreatedAt >= weekAgo);
            NewProjectsThisWeek = newProjectsCount > 0 
                ? $"{newProjectsCount} new this week" 
                : "No new projects";
        }
    }

    private async Task LoadThisMonthExpensesAsync()
    {
        var (startDate, endDate) = GetDateRangeFromFilter();

        var result = await _expenseService.GetExpensesByDateRangeAsync(startDate: startDate, endDate: endDate);

        if (result.Success && result.Data != null)
        {
            // Calculate total based on PaymentDate
            var periodExpenses = result.Data
                .Where(e => e.PaymentDate >= startDate && e.PaymentDate <= endDate)
                .ToList();
                
            _currentMonthTotal = periodExpenses.Sum(e => e.Amount);
            ThisMonthExpenses = _settingsService.FormatCurrency(_currentMonthTotal);
            
            // Set transaction count
            var transactionCount = periodExpenses.Count;
            ThisMonthTransactions = $"{transactionCount} transaction{(transactionCount != 1 ? "s" : "")}";
            
            System.Diagnostics.Debug.WriteLine($"[Dashboard] Period total: {_currentMonthTotal}");
            System.Diagnostics.Debug.WriteLine($"[Dashboard] Total monthly budget: {_totalMonthlyBudget}");
            System.Diagnostics.Debug.WriteLine($"[Dashboard] Expenses in period: {transactionCount}");
            
            // Calculate budget percentage - adjust based on period
            if (_totalMonthlyBudget > 0)
            {
                // Calculate prorated budget based on the period length
                var daysInPeriod = (endDate - startDate).Days + 1;
                var monthsInPeriod = daysInPeriod / 30.0m; // Approximate months
                var proratedBudget = _totalMonthlyBudget * monthsInPeriod;
                
                var percentage = proratedBudget > 0 ? (_currentMonthTotal / proratedBudget) * 100 : 0;
                BudgetUsedPercentage = (double)percentage;
                var remaining = proratedBudget - _currentMonthTotal;
                BudgetRemaining = $"{_settingsService.FormatCurrency(remaining)} remaining";
                
                System.Diagnostics.Debug.WriteLine($"[Dashboard] Budget used percentage: {BudgetUsedPercentage:F2}%");
            }
            else
            {
                BudgetUsedPercentage = 0;
                BudgetRemaining = "No budget set";
                System.Diagnostics.Debug.WriteLine("[Dashboard] No budget set");
            }
        }
    }

    private async Task LoadRecentExpensesAsync()
    {
        var result = await _expenseService.GetAllExpensesAsync();
        if (result.Success && result.Data != null)
        {
            RecentExpenses = new ObservableCollection<Expense>(
                result.Data
                    .OrderByDescending(e => e.PaymentDate)
                    .ThenByDescending(e => e.CreatedAt)
                    .Take(5));
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
