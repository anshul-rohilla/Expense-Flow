using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Expense_Flow.Data;
using Expense_Flow.Models;
using Expense_Flow.Helpers;

namespace Expense_Flow.Services;

public interface ISubscriptionService
{
    Task<ServiceResult<IEnumerable<Subscription>>> GetAllSubscriptionsAsync();
    Task<ServiceResult<Subscription>> GetSubscriptionByIdAsync(int id);
    Task<ServiceResult<Subscription>> CreateSubscriptionAsync(Subscription subscription);
    Task<ServiceResult<Subscription>> UpdateSubscriptionAsync(Subscription subscription);
    Task<ServiceResult<bool>> DeleteSubscriptionAsync(int id);
    Task<ServiceResult<bool>> SubscriptionExistsAsync(string name, int? excludeId = null);
}

public class SubscriptionService : ISubscriptionService
{
    private readonly IRepository<Subscription> _subscriptionRepository;
    private readonly IUserService _userService;

    public SubscriptionService(IRepository<Subscription> subscriptionRepository, IUserService userService)
    {
        _subscriptionRepository = subscriptionRepository;
        _userService = userService;
    }

    public async Task<ServiceResult<IEnumerable<Subscription>>> GetAllSubscriptionsAsync()
    {
        try
        {
            var subscriptions = await _subscriptionRepository.GetAllAsync();
            return ServiceResult<IEnumerable<Subscription>>.SuccessResult(subscriptions.OrderBy(s => s.Name));
        }
        catch (Exception ex)
        {
            return ServiceResult<IEnumerable<Subscription>>.FailureResult($"Error retrieving subscriptions: {ex.Message}");
        }
    }

    public async Task<ServiceResult<Subscription>> GetSubscriptionByIdAsync(int id)
    {
        try
        {
            var subscription = await _subscriptionRepository.GetByIdAsync(id);
            if (subscription == null)
            {
                return ServiceResult<Subscription>.FailureResult("Subscription not found.");
            }
            return ServiceResult<Subscription>.SuccessResult(subscription);
        }
        catch (Exception ex)
        {
            return ServiceResult<Subscription>.FailureResult($"Error retrieving subscription: {ex.Message}");
        }
    }

    public async Task<ServiceResult<Subscription>> CreateSubscriptionAsync(Subscription subscription)
    {
        try
        {
            var validationErrors = await ValidateSubscriptionAsync(subscription);
            if (validationErrors.Any())
            {
                return ServiceResult<Subscription>.FailureResult(validationErrors);
            }

            subscription.CreatedAt = DateTime.Now;
            subscription.CreatedBy = _userService.GetCurrentUsername();
            subscription.ModifiedAt = null;
            subscription.ModifiedBy = null;

            var createdSubscription = await _subscriptionRepository.AddAsync(subscription);
            return ServiceResult<Subscription>.SuccessResult(createdSubscription, "Subscription created successfully.");
        }
        catch (Exception ex)
        {
            return ServiceResult<Subscription>.FailureResult($"Error creating subscription: {ex.Message}");
        }
    }

    public async Task<ServiceResult<Subscription>> UpdateSubscriptionAsync(Subscription subscription)
    {
        try
        {
            var existingSubscription = await _subscriptionRepository.GetByIdAsync(subscription.Id);
            if (existingSubscription == null)
            {
                return ServiceResult<Subscription>.FailureResult("Subscription not found.");
            }

            var validationErrors = await ValidateSubscriptionAsync(subscription, subscription.Id);
            if (validationErrors.Any())
            {
                return ServiceResult<Subscription>.FailureResult(validationErrors);
            }

            existingSubscription.Name = subscription.Name;
            existingSubscription.Type = subscription.Type;
            existingSubscription.Reference = subscription.Reference;
            existingSubscription.ContactId = subscription.ContactId;
            existingSubscription.ModifiedAt = DateTime.Now;
            existingSubscription.ModifiedBy = _userService.GetCurrentUsername();

            var updatedSubscription = await _subscriptionRepository.UpdateAsync(existingSubscription);
            return ServiceResult<Subscription>.SuccessResult(updatedSubscription, "Subscription updated successfully.");
        }
        catch (Exception ex)
        {
            return ServiceResult<Subscription>.FailureResult($"Error updating subscription: {ex.Message}");
        }
    }

    public async Task<ServiceResult<bool>> DeleteSubscriptionAsync(int id)
    {
        try
        {
            var subscription = await _subscriptionRepository.GetByIdAsync(id);
            if (subscription == null)
            {
                return ServiceResult<bool>.FailureResult("Subscription not found.");
            }

            await _subscriptionRepository.DeleteAsync(subscription);
            return ServiceResult<bool>.SuccessResult(true, "Subscription deleted successfully.");
        }
        catch (Exception ex)
        {
            return ServiceResult<bool>.FailureResult($"Error deleting subscription: {ex.Message}");
        }
    }

    public async Task<ServiceResult<bool>> SubscriptionExistsAsync(string name, int? excludeId = null)
    {
        try
        {
            var exists = await _subscriptionRepository.ExistsAsync(s =>
                s.Name.ToLower() == name.ToLower() &&
                (!excludeId.HasValue || s.Id != excludeId.Value));

            return ServiceResult<bool>.SuccessResult(exists);
        }
        catch (Exception ex)
        {
            return ServiceResult<bool>.FailureResult($"Error checking subscription existence: {ex.Message}");
        }
    }

    private async Task<List<string>> ValidateSubscriptionAsync(Subscription subscription, int? excludeId = null)
    {
        var errors = new List<string>();

        errors.AddRange(ValidationHelper.ValidateRequired(subscription.Name, "Name"));
        errors.AddRange(ValidationHelper.ValidateMaxLength(subscription.Name, 200, "Name"));
        errors.AddRange(ValidationHelper.ValidateMaxLength(subscription.Type, 100, "Type"));
        errors.AddRange(ValidationHelper.ValidateMaxLength(subscription.Reference, 200, "Reference"));

        var existsResult = await SubscriptionExistsAsync(subscription.Name, excludeId);
        if (existsResult.Success && existsResult.Data)
        {
            errors.Add($"A subscription with the name '{subscription.Name}' already exists.");
        }

        return errors;
    }
}
