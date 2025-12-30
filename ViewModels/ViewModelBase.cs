using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Expense_Flow.ViewModels;

public abstract partial class ViewModelBase : ObservableObject
{
    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _busyMessage = string.Empty;

    [ObservableProperty]
    private bool _hasErrors;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    protected void SetError(string message)
    {
        HasErrors = true;
        ErrorMessage = message;
    }

    protected void ClearError()
    {
        HasErrors = false;
        ErrorMessage = string.Empty;
    }

    protected async Task ExecuteAsync(Func<Task> operation, string? busyMessage = null)
    {
        IsBusy = true;
        BusyMessage = busyMessage ?? "Loading...";
        ClearError();

        try
        {
            await operation();
        }
        catch (Exception ex)
        {
            SetError(ex.Message);
        }
        finally
        {
            IsBusy = false;
            BusyMessage = string.Empty;
        }
    }
}
