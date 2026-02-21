using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Expense_Flow.Data;
using Expense_Flow.Models;
using Expense_Flow.Helpers;
using Microsoft.EntityFrameworkCore;

namespace Expense_Flow.Services;

public interface IContactService
{
    Task<ServiceResult<IEnumerable<Contact>>> GetAllContactsAsync(int organizationId);
    Task<ServiceResult<IEnumerable<Contact>>> GetTeamMembersAsync(int organizationId);
    Task<ServiceResult<Contact>> GetContactByIdAsync(int id);
    Task<ServiceResult<Contact>> CreateContactAsync(Contact contact);
    Task<ServiceResult<Contact>> UpdateContactAsync(Contact contact);
    Task<ServiceResult<bool>> DeleteContactAsync(int id);
    Task<ServiceResult<bool>> ContactExistsAsync(string name, int organizationId, int? excludeId = null);
}

public class ContactService : IContactService
{
    private readonly IRepository<Contact> _contactRepository;
    private readonly IUserService _userService;

    public ContactService(IRepository<Contact> contactRepository, IUserService userService)
    {
        _contactRepository = contactRepository;
        _userService = userService;
    }

    public async Task<ServiceResult<IEnumerable<Contact>>> GetAllContactsAsync(int organizationId)
    {
        try
        {
            var contacts = await _contactRepository.FindAsync(c => c.OrganizationId == organizationId);
            return ServiceResult<IEnumerable<Contact>>.SuccessResult(contacts.OrderBy(c => c.Name));
        }
        catch (Exception ex)
        {
            return ServiceResult<IEnumerable<Contact>>.FailureResult($"Error retrieving contacts: {ex.Message}");
        }
    }

    public async Task<ServiceResult<IEnumerable<Contact>>> GetTeamMembersAsync(int organizationId)
    {
        try
        {
            var contacts = await _contactRepository.FindAsync(c => 
                c.OrganizationId == organizationId && 
                (c.Role == ContactRole.TeamMember || c.Role == ContactRole.Both));
            return ServiceResult<IEnumerable<Contact>>.SuccessResult(contacts.OrderBy(c => c.Name));
        }
        catch (Exception ex)
        {
            return ServiceResult<IEnumerable<Contact>>.FailureResult($"Error: {ex.Message}");
        }
    }

    public async Task<ServiceResult<Contact>> GetContactByIdAsync(int id)
    {
        try
        {
            var contact = await _contactRepository.GetByIdAsync(id);
            if (contact == null)
            {
                return ServiceResult<Contact>.FailureResult("Contact not found.");
            }
            return ServiceResult<Contact>.SuccessResult(contact);
        }
        catch (Exception ex)
        {
            return ServiceResult<Contact>.FailureResult($"Error retrieving contact: {ex.Message}");
        }
    }

    public async Task<ServiceResult<Contact>> CreateContactAsync(Contact contact)
    {
        try
        {
            var validationErrors = await ValidateContactAsync(contact);
            if (validationErrors.Any())
            {
                return ServiceResult<Contact>.FailureResult(validationErrors);
            }

            contact.CreatedAt = DateTime.Now;
            contact.CreatedBy = _userService.GetCurrentUsername();
            contact.ModifiedAt = null;
            contact.ModifiedBy = null;

            var createdContact = await _contactRepository.AddAsync(contact);
            return ServiceResult<Contact>.SuccessResult(createdContact, "Contact created successfully.");
        }
        catch (Exception ex)
        {
            return ServiceResult<Contact>.FailureResult($"Error creating contact: {ex.Message}");
        }
    }

    public async Task<ServiceResult<Contact>> UpdateContactAsync(Contact contact)
    {
        try
        {
            var existingContact = await _contactRepository.GetByIdAsync(contact.Id);
            if (existingContact == null)
            {
                return ServiceResult<Contact>.FailureResult("Contact not found.");
            }

            var validationErrors = await ValidateContactAsync(contact, contact.Id);
            if (validationErrors.Any())
            {
                return ServiceResult<Contact>.FailureResult(validationErrors);
            }

            existingContact.Name = contact.Name;
            existingContact.Reference = contact.Reference;
            existingContact.Phone = contact.Phone;
            existingContact.Email = contact.Email;
            existingContact.Role = contact.Role;
            existingContact.ModifiedAt = DateTime.Now;
            existingContact.ModifiedBy = _userService.GetCurrentUsername();

            var updatedContact = await _contactRepository.UpdateAsync(existingContact);
            return ServiceResult<Contact>.SuccessResult(updatedContact, "Contact updated successfully.");
        }
        catch (Exception ex)
        {
            return ServiceResult<Contact>.FailureResult($"Error updating contact: {ex.Message}");
        }
    }

    public async Task<ServiceResult<bool>> DeleteContactAsync(int id)
    {
        try
        {
            var contact = await _contactRepository.GetByIdAsync(id);
            if (contact == null)
            {
                return ServiceResult<bool>.FailureResult("Contact not found.");
            }

            await _contactRepository.DeleteAsync(contact);
            return ServiceResult<bool>.SuccessResult(true, "Contact deleted successfully.");
        }
        catch (Exception ex)
        {
            return ServiceResult<bool>.FailureResult($"Error deleting contact: {ex.Message}");
        }
    }

    public async Task<ServiceResult<bool>> ContactExistsAsync(string name, int organizationId, int? excludeId = null)
    {
        try
        {
            var exists = await _contactRepository.ExistsAsync(c =>
                c.Name.ToLower() == name.ToLower() &&
                c.OrganizationId == organizationId &&
                (!excludeId.HasValue || c.Id != excludeId.Value));

            return ServiceResult<bool>.SuccessResult(exists);
        }
        catch (Exception ex)
        {
            return ServiceResult<bool>.FailureResult($"Error checking contact existence: {ex.Message}");
        }
    }

    private async Task<List<string>> ValidateContactAsync(Contact contact, int? excludeId = null)
    {
        var errors = new List<string>();

        errors.AddRange(ValidationHelper.ValidateRequired(contact.Name, "Name"));
        errors.AddRange(ValidationHelper.ValidateMaxLength(contact.Name, 200, "Name"));
        errors.AddRange(ValidationHelper.ValidateMaxLength(contact.Reference, 100, "Reference"));
        errors.AddRange(ValidationHelper.ValidateMaxLength(contact.Phone, 20, "Phone"));
        errors.AddRange(ValidationHelper.ValidateMaxLength(contact.Email, 200, "Email"));

        if (!string.IsNullOrWhiteSpace(contact.Email) && !ValidationHelper.IsValidEmail(contact.Email))
        {
            errors.Add("Email format is invalid.");
        }

        if (!string.IsNullOrWhiteSpace(contact.Phone) && !ValidationHelper.IsValidPhone(contact.Phone))
        {
            errors.Add("Phone number format is invalid.");
        }

        var existsResult = await ContactExistsAsync(contact.Name, contact.OrganizationId, excludeId);
        if (existsResult.Success && existsResult.Data)
        {
            errors.Add($"A contact with the name '{contact.Name}' already exists.");
        }

        return errors;
    }
}
