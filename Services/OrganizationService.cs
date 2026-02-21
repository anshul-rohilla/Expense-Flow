using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Expense_Flow.Data;
using Expense_Flow.Models;

namespace Expense_Flow.Services;

public interface IOrganizationService
{
    Task<ServiceResult<Organization>> GetDefaultOrganizationAsync();
    Task<ServiceResult<Organization>> GetOrganizationByIdAsync(int id);
    Task<ServiceResult<Organization>> UpdateOrganizationAsync(Organization organization);
    Task<ServiceResult<IEnumerable<OrganizationMember>>> GetMembersAsync(int organizationId);
    Task<ServiceResult<OrganizationMember>> AddMemberAsync(int organizationId, int contactId, OrgRole role);
    Task<ServiceResult<bool>> RemoveMemberAsync(int organizationId, int contactId);
    int GetCurrentOrganizationId();
}

public class OrganizationService : IOrganizationService
{
    private readonly IRepository<Organization> _organizationRepository;
    private readonly IRepository<OrganizationMember> _memberRepository;
    private readonly IUserService _userService;
    private int _currentOrgId = 1; // Default

    public OrganizationService(
        IRepository<Organization> organizationRepository,
        IRepository<OrganizationMember> memberRepository,
        IUserService userService)
    {
        _organizationRepository = organizationRepository;
        _memberRepository = memberRepository;
        _userService = userService;
    }

    public int GetCurrentOrganizationId() => _currentOrgId;

    public async Task<ServiceResult<Organization>> GetDefaultOrganizationAsync()
    {
        try
        {
            var orgs = await _organizationRepository.GetAllAsync();
            var org = orgs.FirstOrDefault();
            if (org == null)
                return ServiceResult<Organization>.FailureResult("No organization found.");
            _currentOrgId = org.Id;
            return ServiceResult<Organization>.SuccessResult(org);
        }
        catch (Exception ex)
        {
            return ServiceResult<Organization>.FailureResult($"Error: {ex.Message}");
        }
    }

    public async Task<ServiceResult<Organization>> GetOrganizationByIdAsync(int id)
    {
        try
        {
            var org = await _organizationRepository.GetByIdAsync(id);
            if (org == null)
                return ServiceResult<Organization>.FailureResult("Organization not found.");
            return ServiceResult<Organization>.SuccessResult(org);
        }
        catch (Exception ex)
        {
            return ServiceResult<Organization>.FailureResult($"Error: {ex.Message}");
        }
    }

    public async Task<ServiceResult<Organization>> UpdateOrganizationAsync(Organization organization)
    {
        try
        {
            var existing = await _organizationRepository.GetByIdAsync(organization.Id);
            if (existing == null)
                return ServiceResult<Organization>.FailureResult("Organization not found.");

            existing.Name = organization.Name;
            existing.Description = organization.Description;
            existing.DefaultCurrency = organization.DefaultCurrency;
            existing.ModifiedAt = DateTime.Now;
            existing.ModifiedBy = _userService.GetCurrentUsername();

            var updated = await _organizationRepository.UpdateAsync(existing);
            return ServiceResult<Organization>.SuccessResult(updated, "Organization updated.");
        }
        catch (Exception ex)
        {
            return ServiceResult<Organization>.FailureResult($"Error: {ex.Message}");
        }
    }

    public async Task<ServiceResult<IEnumerable<OrganizationMember>>> GetMembersAsync(int organizationId)
    {
        try
        {
            var members = await _memberRepository.FindAsync(m => m.OrganizationId == organizationId);
            return ServiceResult<IEnumerable<OrganizationMember>>.SuccessResult(members);
        }
        catch (Exception ex)
        {
            return ServiceResult<IEnumerable<OrganizationMember>>.FailureResult($"Error: {ex.Message}");
        }
    }

    public async Task<ServiceResult<OrganizationMember>> AddMemberAsync(int organizationId, int contactId, OrgRole role)
    {
        try
        {
            var exists = await _memberRepository.ExistsAsync(m =>
                m.OrganizationId == organizationId && m.ContactId == contactId);
            if (exists)
                return ServiceResult<OrganizationMember>.FailureResult("Contact is already a member.");

            var member = new OrganizationMember
            {
                OrganizationId = organizationId,
                ContactId = contactId,
                Role = role,
                JoinedAt = DateTime.Now,
                CreatedAt = DateTime.Now,
                CreatedBy = _userService.GetCurrentUsername()
            };

            var created = await _memberRepository.AddAsync(member);
            return ServiceResult<OrganizationMember>.SuccessResult(created, "Member added.");
        }
        catch (Exception ex)
        {
            return ServiceResult<OrganizationMember>.FailureResult($"Error: {ex.Message}");
        }
    }

    public async Task<ServiceResult<bool>> RemoveMemberAsync(int organizationId, int contactId)
    {
        try
        {
            var members = await _memberRepository.FindAsync(m =>
                m.OrganizationId == organizationId && m.ContactId == contactId);
            var member = members.FirstOrDefault();
            if (member == null)
                return ServiceResult<bool>.FailureResult("Member not found.");

            if (member.Role == OrgRole.Owner)
                return ServiceResult<bool>.FailureResult("Cannot remove the owner.");

            await _memberRepository.DeleteAsync(member);
            return ServiceResult<bool>.SuccessResult(true, "Member removed.");
        }
        catch (Exception ex)
        {
            return ServiceResult<bool>.FailureResult($"Error: {ex.Message}");
        }
    }
}
