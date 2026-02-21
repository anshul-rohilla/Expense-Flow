using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Expense_Flow.Models;
using Expense_Flow.Services;

namespace Expense_Flow.ViewModels;

public partial class SubscriptionsViewModel : ViewModelBase
{
    private readonly ISubscriptionService _subscriptionService;
    private readonly IOrganizationService _organizationService;
    private readonly IVendorService _vendorService;

    [ObservableProperty]
    private ObservableCollection<Subscription> _subscriptions = new();

    [ObservableProperty]
    private Subscription? _selectedSubscription;

    [ObservableProperty]
    private string _searchText = string.Empty;

    public SubscriptionsViewModel(
        ISubscriptionService subscriptionService,
        IOrganizationService organizationService,
        IVendorService vendorService)
    {
        _subscriptionService = subscriptionService;
        _organizationService = organizationService;
        _vendorService = vendorService;
    }

    [RelayCommand]
    private async Task LoadSubscriptionsAsync()
    {
        await ExecuteAsync(async () =>
        {
            var orgId = _organizationService.GetCurrentOrganizationId();
            var result = await _subscriptionService.GetAllSubscriptionsAsync(orgId);
            if (result.Success && result.Data != null)
            {
                // Load vendor names for display
                var vendorsResult = await _vendorService.GetAllVendorsAsync(orgId);
                var vendorMap = vendorsResult.Success && vendorsResult.Data != null
                    ? vendorsResult.Data.ToDictionary(v => v.Id, v => v.Name)
                    : new System.Collections.Generic.Dictionary<int, string>();

                foreach (var sub in result.Data)
                {
                    if (vendorMap.TryGetValue(sub.VendorId, out var vendorName))
                    {
                        sub.SetVendorName(vendorName);
                    }
                }

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
