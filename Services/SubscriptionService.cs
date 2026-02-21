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
    Task<ServiceResult<IEnumerable<Subscription>>> GetAllSubscriptionsAsync(int organizationId);
    Task<ServiceResult<IEnumerable<Subscription>>> GetSubscriptionsByVendorAsync(int vendorId);
    Task<ServiceResult<IEnumerable<Subscription>>> GetSubscriptionsByProjectAsync(int projectId);
    Task<ServiceResult<Subscription>> GetSubscriptionByIdAsync(int id);
    Task<ServiceResult<Subscription>> CreateSubscriptionAsync(Subscription subscription);
    Task<ServiceResult<Subscription>> UpdateSubscriptionAsync(Subscription subscription);
    Task<ServiceResult<bool>> DeleteSubscriptionAsync(int id);
    Task<ServiceResult<bool>> LinkSubscriptionToProjectAsync(int subscriptionId, int projectId);
    Task<ServiceResult<bool>> UnlinkSubscriptionFromProjectAsync(int subscriptionId, int projectId);
}

public class SubscriptionService : ISubscriptionService
{
    private readonly IRepository<Subscription> _subscriptionRepository;
    private readonly IRepository<ProjectSubscription> _projectSubscriptionRepository;
    private readonly IUserService _userService;

    public SubscriptionService(
        IRepository<Subscription> subscriptionRepository,
        IRepository<ProjectSubscription> projectSubscriptionRepository,
        IUserService userService)
    {
        _subscriptionRepository = subscriptionRepository;
        _projectSubscriptionRepository = projectSubscriptionRepository;
        _userService = userService;
    }

    public async Task<ServiceResult<IEnumerable<Subscription>>> GetAllSubscriptionsAsync(int organizationId)
    {
        try
        {
            var subscriptions = await _subscriptionRepository.FindAsync(s => s.OrganizationId == organizationId);
            return ServiceResult<IEnumerable<Subscription>>.SuccessResult(subscriptions.OrderBy(s => s.Name));
        }
        catch (Exception ex)
        {
            return ServiceResult<IEnumerable<Subscription>>.FailureResult($"Error: {ex.Message}");
        }
    }

    public async Task<ServiceResult<IEnumerable<Subscription>>> GetSubscriptionsByVendorAsync(int vendorId)
    {
        try
        {
            var subscriptions = await _subscriptionRepository.FindAsync(s => s.VendorId == vendorId);
            return ServiceResult<IEnumerable<Subscription>>.SuccessResult(subscriptions.OrderBy(s => s.Name));
        }
        catch (Exception ex)
        {
            return ServiceResult<IEnumerable<Subscription>>.FailureResult($"Error: {ex.Message}");
        }
    }

    public async Task<ServiceResult<IEnumerable<Subscription>>> GetSubscriptionsByProjectAsync(int projectId)
    {
        try
        {
            var projectSubs = await _projectSubscriptionRepository.FindAsync(ps => ps.ProjectId == projectId);
            var subIds = projectSubs.Select(ps => ps.SubscriptionId).ToList();
            var subscriptions = await _subscriptionRepository.FindAsync(s => subIds.Contains(s.Id));
            return ServiceResult<IEnumerable<Subscription>>.SuccessResult(subscriptions.OrderBy(s => s.Name));
        }
        catch (Exception ex)
        {
            return ServiceResult<IEnumerable<Subscription>>.FailureResult($"Error: {ex.Message}");
        }
    }

    public async Task<ServiceResult<Subscription>> GetSubscriptionByIdAsync(int id)
    {
        try
        {
            var subscription = await _subscriptionRepository.GetByIdAsync(id);
            if (subscription == null)
                return ServiceResult<Subscription>.FailureResult("Subscription not found.");
            return ServiceResult<Subscription>.SuccessResult(subscription);
        }
        catch (Exception ex)
        {
            return ServiceResult<Subscription>.FailureResult($"Error: {ex.Message}");
        }
    }

    public async Task<ServiceResult<Subscription>> CreateSubscriptionAsync(Subscription subscription)
    {
        try
        {
            var errors = ValidateSubscription(subscription);
            if (errors.Any())
                return ServiceResult<Subscription>.FailureResult(errors);

            subscription.CreatedAt = DateTime.Now;
            subscription.CreatedBy = _userService.GetCurrentUsername();

            var created = await _subscriptionRepository.AddAsync(subscription);
            return ServiceResult<Subscription>.SuccessResult(created, "Subscription created.");
        }
        catch (Exception ex)
        {
            return ServiceResult<Subscription>.FailureResult($"Error: {ex.Message}");
        }
    }

    public async Task<ServiceResult<Subscription>> UpdateSubscriptionAsync(Subscription subscription)
    {
        try
        {
            var existing = await _subscriptionRepository.GetByIdAsync(subscription.Id);
            if (existing == null)
                return ServiceResult<Subscription>.FailureResult("Subscription not found.");

            var errors = ValidateSubscription(subscription);
            if (errors.Any())
                return ServiceResult<Subscription>.FailureResult(errors);

            existing.Name = subscription.Name;
            existing.VendorId = subscription.VendorId;
            existing.Plan = subscription.Plan;
            existing.Amount = subscription.Amount;
            existing.Currency = subscription.Currency;
            existing.BillingCycle = subscription.BillingCycle;
            existing.StartDate = subscription.StartDate;
            existing.RenewalDate = subscription.RenewalDate;
            existing.IsActive = subscription.IsActive;
            existing.Reference = subscription.Reference;
            existing.Notes = subscription.Notes;
            existing.ModifiedAt = DateTime.Now;
            existing.ModifiedBy = _userService.GetCurrentUsername();

            var updated = await _subscriptionRepository.UpdateAsync(existing);
            return ServiceResult<Subscription>.SuccessResult(updated, "Subscription updated.");
        }
        catch (Exception ex)
        {
            return ServiceResult<Subscription>.FailureResult($"Error: {ex.Message}");
        }
    }

    public async Task<ServiceResult<bool>> DeleteSubscriptionAsync(int id)
    {
        try
        {
            var subscription = await _subscriptionRepository.GetByIdAsync(id);
            if (subscription == null)
                return ServiceResult<bool>.FailureResult("Subscription not found.");

            await _subscriptionRepository.DeleteAsync(subscription);
            return ServiceResult<bool>.SuccessResult(true, "Subscription deleted.");
        }
        catch (Exception ex)
        {
            return ServiceResult<bool>.FailureResult($"Error: {ex.Message}");
        }
    }

    public async Task<ServiceResult<bool>> LinkSubscriptionToProjectAsync(int subscriptionId, int projectId)
    {
        try
        {
            var exists = await _projectSubscriptionRepository.ExistsAsync(ps =>
                ps.SubscriptionId == subscriptionId && ps.ProjectId == projectId);
            if (exists)
                return ServiceResult<bool>.FailureResult("Already linked.");

            var link = new ProjectSubscription
            {
                ProjectId = projectId,
                SubscriptionId = subscriptionId,
                CreatedAt = DateTime.Now,
                CreatedBy = _userService.GetCurrentUsername()
            };
            await _projectSubscriptionRepository.AddAsync(link);
            return ServiceResult<bool>.SuccessResult(true, "Subscription linked.");
        }
        catch (Exception ex)
        {
            return ServiceResult<bool>.FailureResult($"Error: {ex.Message}");
        }
    }

    public async Task<ServiceResult<bool>> UnlinkSubscriptionFromProjectAsync(int subscriptionId, int projectId)
    {
        try
        {
            var links = await _projectSubscriptionRepository.FindAsync(ps =>
                ps.SubscriptionId == subscriptionId && ps.ProjectId == projectId);
            var link = links.FirstOrDefault();
            if (link == null)
                return ServiceResult<bool>.FailureResult("Link not found.");

            await _projectSubscriptionRepository.DeleteAsync(link);
            return ServiceResult<bool>.SuccessResult(true, "Subscription unlinked.");
        }
        catch (Exception ex)
        {
            return ServiceResult<bool>.FailureResult($"Error: {ex.Message}");
        }
    }

    private List<string> ValidateSubscription(Subscription subscription)
    {
        var errors = new List<string>();
        errors.AddRange(ValidationHelper.ValidateRequired(subscription.Name, "Name"));
        errors.AddRange(ValidationHelper.ValidateMaxLength(subscription.Name, 200, "Name"));

        if (subscription.VendorId <= 0)
            errors.Add("Vendor is required.");

        if (subscription.Amount.HasValue && subscription.Amount.Value < 0)
            errors.Add("Amount must be zero or positive.");

        return errors;
    }
}
