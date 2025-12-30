using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;
using Expense_Flow.Models;
using Expense_Flow.Services;

namespace Expense_Flow.ViewModels;

public partial class SettingsViewModel : ViewModelBase
{
    private readonly IExpenseTypeService _expenseTypeService;
    private readonly ISettingsService _settingsService;

    [ObservableProperty]
    private ElementTheme _selectedTheme = ElementTheme.Default;

    [ObservableProperty]
    private string _selectedBackdrop = "Mica";

    [ObservableProperty]
    private string _selectedCurrency = "USD";

    [ObservableProperty]
    private string _selectedDateFormat = "MM/DD/YYYY";

    [ObservableProperty]
    private bool _launchOnStartup = false;

    [ObservableProperty]
    private string _databasePath = string.Empty;

    [ObservableProperty]
    private string _appVersion = "1.0.0";

    [ObservableProperty]
    private ObservableCollection<ExpenseType> _expenseTypes = new();

    public SettingsViewModel(IExpenseTypeService expenseTypeService, ISettingsService settingsService)
    {
        _expenseTypeService = expenseTypeService;
        _settingsService = settingsService;
        LoadSettings();
        _ = LoadExpenseTypesAsync();
    }

    private void LoadSettings()
    {
        // Load settings from settings service
        DatabasePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ExpenseFlow",
            "expenseflow.db");

        SelectedTheme = _settingsService.Theme;
        SelectedBackdrop = _settingsService.Backdrop;
        SelectedCurrency = _settingsService.Currency;
        SelectedDateFormat = _settingsService.DateFormat;
        LaunchOnStartup = _settingsService.LaunchOnStartup;
    }

    [RelayCommand]
    private async Task LoadExpenseTypesAsync()
    {
        await ExecuteAsync(async () =>
        {
            var result = await _expenseTypeService.GetAllExpenseTypesAsync();
            if (result.Success && result.Data != null)
            {
                ExpenseTypes = new ObservableCollection<ExpenseType>(result.Data);
            }
            else
            {
                SetError(result.GetErrorMessage());
            }
        }, "Loading expense types...");
    }

    [RelayCommand]
    private async Task DeleteExpenseTypeAsync(ExpenseType expenseType)
    {
        if (expenseType == null) return;

        await ExecuteAsync(async () =>
        {
            var result = await _expenseTypeService.DeleteExpenseTypeAsync(expenseType.Id);
            if (result.Success)
            {
                ExpenseTypes.Remove(expenseType);
            }
            else
            {
                SetError(result.GetErrorMessage());
            }
        }, "Deleting expense type...");
    }

    [RelayCommand]
    private void ChangeTheme(string theme)
    {
        SelectedTheme = theme switch
        {
            "Light" => ElementTheme.Light,
            "Dark" => ElementTheme.Dark,
            _ => ElementTheme.Default
        };

        _settingsService.Theme = SelectedTheme;
        SaveSettings();
    }

    [RelayCommand]
    private void ChangeBackdrop(string backdrop)
    {
        SelectedBackdrop = backdrop;
        _settingsService.Backdrop = backdrop;
        SaveSettings();
    }

    [RelayCommand]
    private void ChangeCurrency(string currency)
    {
        SelectedCurrency = currency;
        _settingsService.Currency = currency;
        SaveSettings();
    }

    [RelayCommand]
    private void ChangeDateFormat(string format)
    {
        SelectedDateFormat = format;
        _settingsService.DateFormat = format;
        SaveSettings();
    }

    [RelayCommand]
    private void ToggleLaunchOnStartup()
    {
        _settingsService.LaunchOnStartup = LaunchOnStartup;
        SaveSettings();
    }

    [RelayCommand]
    private async Task BackupDataAsync()
    {
        await ExecuteAsync(async () =>
        {
            // Implement backup logic
            await Task.Delay(1000); // Placeholder
            // Show success message
        }, "Backing up data...");
    }

    [RelayCommand]
    private async Task RestoreDataAsync()
    {
        await ExecuteAsync(async () =>
        {
            // Implement restore logic
            await Task.Delay(1000); // Placeholder
            // Show success message
        }, "Restoring data...");
    }

    [RelayCommand]
    private async Task ExportDataAsync()
    {
        await ExecuteAsync(async () =>
        {
            // Implement export logic
            await Task.Delay(1000); // Placeholder
            // Show success message
        }, "Exporting data...");
    }

    private void SaveSettings()
    {
        _settingsService.SaveSettings();
    }
}
