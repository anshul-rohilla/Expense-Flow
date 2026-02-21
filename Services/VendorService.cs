using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Expense_Flow.Data;
using Expense_Flow.Models;
using Expense_Flow.Helpers;

namespace Expense_Flow.Services;

public interface IVendorService
{
    Task<ServiceResult<IEnumerable<Vendor>>> GetAllVendorsAsync(int organizationId);
    Task<ServiceResult<IEnumerable<Vendor>>> GetVendorsByProjectAsync(int projectId);
    Task<ServiceResult<Vendor>> GetVendorByIdAsync(int id);
    Task<ServiceResult<Vendor>> CreateVendorAsync(Vendor vendor);
    Task<ServiceResult<Vendor>> UpdateVendorAsync(Vendor vendor);
    Task<ServiceResult<bool>> DeleteVendorAsync(int id);
    Task<ServiceResult<bool>> LinkVendorToProjectAsync(int vendorId, int projectId, string? accountId = null);
    Task<ServiceResult<bool>> UnlinkVendorFromProjectAsync(int vendorId, int projectId);
    Task<ServiceResult<IEnumerable<ProjectVendor>>> GetProjectVendorsAsync(int projectId);
    Task<ServiceResult<bool>> VendorExistsAsync(string name, int organizationId, int? excludeId = null);
}

public class VendorService : IVendorService
{
    private readonly IRepository<Vendor> _vendorRepository;
    private readonly IRepository<ProjectVendor> _projectVendorRepository;
    private readonly IRepository<Project> _projectRepository;
    private readonly IUserService _userService;

    public VendorService(
        IRepository<Vendor> vendorRepository,
        IRepository<ProjectVendor> projectVendorRepository,
        IRepository<Project> projectRepository,
        IUserService userService)
    {
        _vendorRepository = vendorRepository;
        _projectVendorRepository = projectVendorRepository;
        _projectRepository = projectRepository;
        _userService = userService;
    }

    public async Task<ServiceResult<IEnumerable<Vendor>>> GetAllVendorsAsync(int organizationId)
    {
        try
        {
            var vendors = await _vendorRepository.FindAsync(v => v.OrganizationId == organizationId && !v.IsArchived);
            return ServiceResult<IEnumerable<Vendor>>.SuccessResult(vendors.OrderBy(v => v.Name));
        }
        catch (Exception ex)
        {
            return ServiceResult<IEnumerable<Vendor>>.FailureResult($"Error retrieving vendors: {ex.Message}");
        }
    }

    public async Task<ServiceResult<IEnumerable<Vendor>>> GetVendorsByProjectAsync(int projectId)
    {
        try
        {
            var projectVendors = await _projectVendorRepository.FindAsync(pv => pv.ProjectId == projectId);
            var vendorIds = projectVendors.Select(pv => pv.VendorId).ToList();
            var vendors = await _vendorRepository.FindAsync(v => vendorIds.Contains(v.Id) && !v.IsArchived);
            return ServiceResult<IEnumerable<Vendor>>.SuccessResult(vendors.OrderBy(v => v.Name));
        }
        catch (Exception ex)
        {
            return ServiceResult<IEnumerable<Vendor>>.FailureResult($"Error: {ex.Message}");
        }
    }

    public async Task<ServiceResult<Vendor>> GetVendorByIdAsync(int id)
    {
        try
        {
            var vendor = await _vendorRepository.GetByIdAsync(id);
            if (vendor == null)
                return ServiceResult<Vendor>.FailureResult("Vendor not found.");
            return ServiceResult<Vendor>.SuccessResult(vendor);
        }
        catch (Exception ex)
        {
            return ServiceResult<Vendor>.FailureResult($"Error: {ex.Message}");
        }
    }

    public async Task<ServiceResult<Vendor>> CreateVendorAsync(Vendor vendor)
    {
        try
        {
            var errors = await ValidateVendorAsync(vendor);
            if (errors.Any())
                return ServiceResult<Vendor>.FailureResult(errors);

            vendor.CreatedAt = DateTime.Now;
            vendor.CreatedBy = _userService.GetCurrentUsername();

            var created = await _vendorRepository.AddAsync(vendor);
            return ServiceResult<Vendor>.SuccessResult(created, "Vendor created successfully.");
        }
        catch (Exception ex)
        {
            return ServiceResult<Vendor>.FailureResult($"Error: {ex.Message}");
        }
    }

    public async Task<ServiceResult<Vendor>> UpdateVendorAsync(Vendor vendor)
    {
        try
        {
            var existing = await _vendorRepository.GetByIdAsync(vendor.Id);
            if (existing == null)
                return ServiceResult<Vendor>.FailureResult("Vendor not found.");

            var errors = await ValidateVendorAsync(vendor, vendor.Id);
            if (errors.Any())
                return ServiceResult<Vendor>.FailureResult(errors);

            existing.Name = vendor.Name;
            existing.Website = vendor.Website;
            existing.AccountReference = vendor.AccountReference;
            existing.Notes = vendor.Notes;
            existing.ContactId = vendor.ContactId;
            existing.ModifiedAt = DateTime.Now;
            existing.ModifiedBy = _userService.GetCurrentUsername();

            var updated = await _vendorRepository.UpdateAsync(existing);
            return ServiceResult<Vendor>.SuccessResult(updated, "Vendor updated successfully.");
        }
        catch (Exception ex)
        {
            return ServiceResult<Vendor>.FailureResult($"Error: {ex.Message}");
        }
    }

    public async Task<ServiceResult<bool>> DeleteVendorAsync(int id)
    {
        try
        {
            var vendor = await _vendorRepository.GetByIdAsync(id);
            if (vendor == null)
                return ServiceResult<bool>.FailureResult("Vendor not found.");

            await _vendorRepository.DeleteAsync(vendor);
            return ServiceResult<bool>.SuccessResult(true, "Vendor deleted.");
        }
        catch (Exception ex)
        {
            return ServiceResult<bool>.FailureResult($"Error: {ex.Message}");
        }
    }

    public async Task<ServiceResult<bool>> LinkVendorToProjectAsync(int vendorId, int projectId, string? accountId = null)
    {
        try
        {
            var exists = await _projectVendorRepository.ExistsAsync(pv =>
                pv.VendorId == vendorId && pv.ProjectId == projectId);
            if (exists)
                return ServiceResult<bool>.FailureResult("Vendor is already linked to this project.");

            var link = new ProjectVendor
            {
                ProjectId = projectId,
                VendorId = vendorId,
                ProjectSpecificAccountId = accountId,
                CreatedAt = DateTime.Now,
                CreatedBy = _userService.GetCurrentUsername()
            };

            await _projectVendorRepository.AddAsync(link);
            return ServiceResult<bool>.SuccessResult(true, "Vendor linked.");
        }
        catch (Exception ex)
        {
            return ServiceResult<bool>.FailureResult($"Error: {ex.Message}");
        }
    }

    public async Task<ServiceResult<bool>> UnlinkVendorFromProjectAsync(int vendorId, int projectId)
    {
        try
        {
            var links = await _projectVendorRepository.FindAsync(pv =>
                pv.VendorId == vendorId && pv.ProjectId == projectId);
            var link = links.FirstOrDefault();
            if (link == null)
                return ServiceResult<bool>.FailureResult("Link not found.");

            await _projectVendorRepository.DeleteAsync(link);
            return ServiceResult<bool>.SuccessResult(true, "Vendor unlinked.");
        }
        catch (Exception ex)
        {
            return ServiceResult<bool>.FailureResult($"Error: {ex.Message}");
        }
    }

    public async Task<ServiceResult<IEnumerable<ProjectVendor>>> GetProjectVendorsAsync(int projectId)
    {
        try
        {
            var pvs = await _projectVendorRepository.FindAsync(pv => pv.ProjectId == projectId);
            return ServiceResult<IEnumerable<ProjectVendor>>.SuccessResult(pvs);
        }
        catch (Exception ex)
        {
            return ServiceResult<IEnumerable<ProjectVendor>>.FailureResult($"Error: {ex.Message}");
        }
    }

    public async Task<ServiceResult<bool>> VendorExistsAsync(string name, int organizationId, int? excludeId = null)
    {
        try
        {
            var exists = await _vendorRepository.ExistsAsync(v =>
                v.Name.ToLower() == name.ToLower() &&
                v.OrganizationId == organizationId &&
                (!excludeId.HasValue || v.Id != excludeId.Value));
            return ServiceResult<bool>.SuccessResult(exists);
        }
        catch (Exception ex)
        {
            return ServiceResult<bool>.FailureResult($"Error: {ex.Message}");
        }
    }

    private async Task<List<string>> ValidateVendorAsync(Vendor vendor, int? excludeId = null)
    {
        var errors = new List<string>();
        errors.AddRange(ValidationHelper.ValidateRequired(vendor.Name, "Name"));
        errors.AddRange(ValidationHelper.ValidateMaxLength(vendor.Name, 200, "Name"));
        errors.AddRange(ValidationHelper.ValidateMaxLength(vendor.Website, 500, "Website"));
        errors.AddRange(ValidationHelper.ValidateMaxLength(vendor.AccountReference, 200, "Account Reference"));

        var existsResult = await VendorExistsAsync(vendor.Name, vendor.OrganizationId, excludeId);
        if (existsResult.Success && existsResult.Data)
            errors.Add($"A vendor with the name '{vendor.Name}' already exists.");

        return errors;
    }
}
