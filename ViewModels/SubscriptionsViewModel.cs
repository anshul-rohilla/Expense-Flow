using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Expense_Flow.Models;
using Expense_Flow.Services;

namespace Expense_Flow.ViewModels;

public partial class SubscriptionsViewModel : ViewModelBase
{
    private readonly ISubscriptionService _subscriptionService;

    [ObservableProperty]
    private ObservableCollection<Subscription> _subscriptions = new();

    [ObservableProperty]
    private Subscription? _selectedSubscription;

    [ObservableProperty]
    private string _searchText = string.Empty;

    public SubscriptionsViewModel(ISubscriptionService SubscriptionService)
    {
        _subscriptionService = SubscriptionService;
    }

    [RelayCommand]
    private async Task LoadSubscriptionsAsync()
    {
        await ExecuteAsync(async () =>
        {
            var result = await _subscriptionService.GetAllSubscriptionsAsync();
            if (result.Success && result.Data != null)
            {
                Subscriptions = new ObservableCollection<Subscription>(result.Data);
            }
            else
            {
                SetError(result.GetErrorMessage());
            }
        }, "Loading Subscriptions...");
    }

    [RelayCommand]
    private async Task DeleteSubscriptionAsync(Subscription Subscription)
    {
        if (Subscription == null) return;

        await ExecuteAsync(async () =>
        {
            var result = await _subscriptionService.DeleteSubscriptionAsync(Subscription.Id);
            if (result.Success)
            {
                Subscriptions.Remove(Subscription);
            }
            else
            {
                SetError(result.GetErrorMessage());
            }
        }, "Deleting Subscription...");
    }

    [RelayCommand]
    private void SearchSubscriptions()
    {
        // Will be implemented with filtering
    }
}
