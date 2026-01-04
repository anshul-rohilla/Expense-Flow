using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;
using Microsoft.Extensions.DependencyInjection;
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
    private string _currentUsername = string.Empty;

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

        // Get app version from package
        try
        {
            var package = Windows.ApplicationModel.Package.Current;
            var version = package.Id.Version;
            AppVersion = $"{version.Major}.{version.Minor}.{version.Build}";
        }
        catch
        {
            AppVersion = "1.0.0";
        }

        // Get current Windows username
        CurrentUsername = Environment.UserName;

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
            try
            {
                var savePicker = new Windows.Storage.Pickers.FileSavePicker();
                savePicker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary;
                savePicker.FileTypeChoices.Add("SQLite Database", new List<string>() { ".db" });
                savePicker.SuggestedFileName = $"ExpenseFlow_Backup_{DateTime.Now:yyyyMMdd_HHmmss}";

                // Get window handle
                var window = (Microsoft.UI.Xaml.Application.Current as App)?.Window;
                if (window != null)
                {
                    var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
                    WinRT.Interop.InitializeWithWindow.Initialize(savePicker, hwnd);
                }

                var file = await savePicker.PickSaveFileAsync();
                if (file != null)
                {
                    var sourceFile = await Windows.Storage.StorageFile.GetFileFromPathAsync(DatabasePath);
                    await sourceFile.CopyAndReplaceAsync(file);

                    // Success - will be shown by base class
                    System.Diagnostics.Debug.WriteLine($"[Settings] Database backed up to: {file.Path}");
                }
            }
            catch (Exception ex)
            {
                SetError($"Backup failed: {ex.Message}");
            }
        }, "Backing up data...");
    }

    [RelayCommand]
    private async Task RestoreDataAsync()
    {
        await ExecuteAsync(async () =>
        {
            try
            {
                var openPicker = new Windows.Storage.Pickers.FileOpenPicker();
                openPicker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary;
                openPicker.FileTypeFilter.Add(".db");

                // Get window handle
                var window = (Microsoft.UI.Xaml.Application.Current as App)?.Window;
                if (window != null)
                {
                    var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
                    WinRT.Interop.InitializeWithWindow.Initialize(openPicker, hwnd);
                }

                var file = await openPicker.PickSingleFileAsync();
                if (file != null)
                {
                    var destinationFile = await Windows.Storage.StorageFile.GetFileFromPathAsync(DatabasePath);
                    await file.CopyAndReplaceAsync(destinationFile);

                    // Success - app should restart
                    System.Diagnostics.Debug.WriteLine($"[Settings] Database restored from: {file.Path}");
                }
            }
            catch (Exception ex)
            {
                SetError($"Restore failed: {ex.Message}");
            }
        }, "Restoring data...");
    }

    [RelayCommand]
    private async Task ClearAllDataAsync(string usernameConfirmation)
    {
        await ExecuteAsync(async () =>
        {
            if (string.IsNullOrWhiteSpace(usernameConfirmation))
            {
                SetError("Username confirmation is required.");
                return;
            }

            if (!usernameConfirmation.Equals(CurrentUsername, StringComparison.OrdinalIgnoreCase))
            {
                SetError($"Username does not match. Please enter '{CurrentUsername}' to confirm.");
                return;
            }

            try
            {
                // Close any open connections
                var dbContext = App.Host!.Services.GetRequiredService<Data.ExpenseFlowDbContext>();
                await dbContext.Database.EnsureDeletedAsync();
                await dbContext.Database.EnsureCreatedAsync();

                // Reinitialize database
                var databaseService = App.Host!.Services.GetRequiredService<DatabaseService>();
                await databaseService.InitializeAsync();

                System.Diagnostics.Debug.WriteLine("[Settings] All data cleared successfully");
            }
            catch (Exception ex)
            {
                SetError($"Clear data failed: {ex.Message}");
            }
        }, "Clearing all data...");
    }

    private void SaveSettings()
    {
        _settingsService.SaveSettings();
    }
}
