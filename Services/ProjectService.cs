using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Expense_Flow.Data;
using Expense_Flow.Models;
using Expense_Flow.Helpers;

namespace Expense_Flow.Services;

public interface IProjectService
{
    Task<ServiceResult<IEnumerable<Project>>> GetAllProjectsAsync(bool includeArchived = false);
    Task<ServiceResult<Project>> GetProjectByIdAsync(int id);
    Task<ServiceResult<Project>> GetDefaultProjectAsync();
    Task<ServiceResult<Project>> CreateProjectAsync(Project project);
    Task<ServiceResult<Project>> UpdateProjectAsync(Project project);
    Task<ServiceResult<bool>> DeleteProjectAsync(int id);
    Task<ServiceResult<bool>> ArchiveProjectAsync(int id);
    Task<ServiceResult<bool>> UnarchiveProjectAsync(int id);
    Task<ServiceResult<bool>> ProjectExistsAsync(string name, int? excludeId = null);
}

public class ProjectService : IProjectService
{
    private readonly IRepository<Project> _projectRepository;
    private readonly IUserService _userService;

    public ProjectService(IRepository<Project> projectRepository, IUserService userService)
    {
        _projectRepository = projectRepository;
        _userService = userService;
    }

    public async Task<ServiceResult<IEnumerable<Project>>> GetAllProjectsAsync(bool includeArchived = false)
    {
        try
        {
            var projects = includeArchived
                ? await _projectRepository.GetAllAsync()
                : await _projectRepository.FindAsync(p => !p.IsArchived);

            return ServiceResult<IEnumerable<Project>>.SuccessResult(projects.OrderBy(p => p.Name));
        }
        catch (Exception ex)
        {
            return ServiceResult<IEnumerable<Project>>.FailureResult($"Error retrieving projects: {ex.Message}");
        }
    }

    public async Task<ServiceResult<Project>> GetProjectByIdAsync(int id)
    {
        try
        {
            var project = await _projectRepository.GetByIdAsync(id);
            if (project == null)
            {
                return ServiceResult<Project>.FailureResult("Project not found.");
            }
            return ServiceResult<Project>.SuccessResult(project);
        }
        catch (Exception ex)
        {
            return ServiceResult<Project>.FailureResult($"Error retrieving project: {ex.Message}");
        }
    }

    public async Task<ServiceResult<Project>> GetDefaultProjectAsync()
    {
        try
        {
            var projects = await _projectRepository.FindAsync(p => p.IsDefault);
            var defaultProject = projects.FirstOrDefault();
            
            if (defaultProject == null)
            {
                return ServiceResult<Project>.FailureResult("Default project not found.");
            }
            
            return ServiceResult<Project>.SuccessResult(defaultProject);
        }
        catch (Exception ex)
        {
            return ServiceResult<Project>.FailureResult($"Error retrieving default project: {ex.Message}");
        }
    }

    public async Task<ServiceResult<Project>> CreateProjectAsync(Project project)
    {
        try
        {
            var validationErrors = await ValidateProjectAsync(project);
            if (validationErrors.Any())
            {
                return ServiceResult<Project>.FailureResult(validationErrors);
            }

            project.IsDefault = false;
            project.IsArchived = false;
            project.CreatedAt = DateTime.Now;
            project.CreatedBy = _userService.GetCurrentUsername();
            project.ModifiedAt = null;
            project.ModifiedBy = null;

            var createdProject = await _projectRepository.AddAsync(project);
            return ServiceResult<Project>.SuccessResult(createdProject, "Project created successfully.");
        }
        catch (Exception ex)
        {
            return ServiceResult<Project>.FailureResult($"Error creating project: {ex.Message}");
        }
    }

    public async Task<ServiceResult<Project>> UpdateProjectAsync(Project project)
    {
        try
        {
            var existingProject = await _projectRepository.GetByIdAsync(project.Id);
            if (existingProject == null)
            {
                return ServiceResult<Project>.FailureResult("Project not found.");
            }

            if (existingProject.IsDefault && project.Name != existingProject.Name)
            {
                return ServiceResult<Project>.FailureResult("Cannot rename the default project.");
            }

            var validationErrors = await ValidateProjectAsync(project, project.Id);
            if (validationErrors.Any())
            {
                return ServiceResult<Project>.FailureResult(validationErrors);
            }

            existingProject.Name = project.Name;
            existingProject.Description = project.Description;
            existingProject.DefaultPaymentModeId = project.DefaultPaymentModeId;
            existingProject.MonthlyBudget = project.MonthlyBudget;
            existingProject.ModifiedAt = DateTime.Now;
            existingProject.ModifiedBy = _userService.GetCurrentUsername();

            var updatedProject = await _projectRepository.UpdateAsync(existingProject);
            return ServiceResult<Project>.SuccessResult(updatedProject, "Project updated successfully.");
        }
        catch (Exception ex)
        {
            return ServiceResult<Project>.FailureResult($"Error updating project: {ex.Message}");
        }
    }

    public async Task<ServiceResult<bool>> DeleteProjectAsync(int id)
    {
        try
        {
            var project = await _projectRepository.GetByIdAsync(id);
            if (project == null)
            {
                return ServiceResult<bool>.FailureResult("Project not found.");
            }

            if (project.IsDefault)
            {
                return ServiceResult<bool>.FailureResult("Cannot delete the default project.");
            }

            // Additional protection for "Personal" project
            if (project.Name.Equals("Personal", StringComparison.OrdinalIgnoreCase))
            {
                return ServiceResult<bool>.FailureResult("Cannot delete the Personal project.");
            }

            await _projectRepository.DeleteAsync(project);
            return ServiceResult<bool>.SuccessResult(true, "Project deleted successfully.");
        }
        catch (Exception ex)
        {
            return ServiceResult<bool>.FailureResult($"Error deleting project: {ex.Message}");
        }
    }

    public async Task<ServiceResult<bool>> ArchiveProjectAsync(int id)
    {
        try
        {
            var project = await _projectRepository.GetByIdAsync(id);
            if (project == null)
            {
                return ServiceResult<bool>.FailureResult("Project not found.");
            }

            if (project.IsDefault)
            {
                return ServiceResult<bool>.FailureResult("Cannot archive the default project.");
            }

            project.IsArchived = true;
            project.ModifiedAt = DateTime.Now;
            project.ModifiedBy = _userService.GetCurrentUsername();

            await _projectRepository.UpdateAsync(project);
            return ServiceResult<bool>.SuccessResult(true, "Project archived successfully.");
        }
        catch (Exception ex)
        {
            return ServiceResult<bool>.FailureResult($"Error archiving project: {ex.Message}");
        }
    }

    public async Task<ServiceResult<bool>> UnarchiveProjectAsync(int id)
    {
        try
        {
            var project = await _projectRepository.GetByIdAsync(id);
            if (project == null)
            {
                return ServiceResult<bool>.FailureResult("Project not found.");
            }

            project.IsArchived = false;
            project.ModifiedAt = DateTime.Now;
            project.ModifiedBy = _userService.GetCurrentUsername();

            await _projectRepository.UpdateAsync(project);
            return ServiceResult<bool>.SuccessResult(true, "Project unarchived successfully.");
        }
        catch (Exception ex)
        {
            return ServiceResult<bool>.FailureResult($"Error unarchiving project: {ex.Message}");
        }
    }

    public async Task<ServiceResult<bool>> ProjectExistsAsync(string name, int? excludeId = null)
    {
        try
        {
            var exists = await _projectRepository.ExistsAsync(p =>
                p.Name.ToLower() == name.ToLower() &&
                (!excludeId.HasValue || p.Id != excludeId.Value));

            return ServiceResult<bool>.SuccessResult(exists);
        }
        catch (Exception ex)
        {
            return ServiceResult<bool>.FailureResult($"Error checking project existence: {ex.Message}");
        }
    }

    private async Task<List<string>> ValidateProjectAsync(Project project, int? excludeId = null)
    {
        var errors = new List<string>();

        errors.AddRange(ValidationHelper.ValidateRequired(project.Name, "Name"));
        errors.AddRange(ValidationHelper.ValidateMaxLength(project.Name, 200, "Name"));
        errors.AddRange(ValidationHelper.ValidateMaxLength(project.Description, 1000, "Description"));
        errors.AddRange(ValidationHelper.ValidatePositive(project.MonthlyBudget, "Monthly Budget"));

        var existsResult = await ProjectExistsAsync(project.Name, excludeId);
        if (existsResult.Success && existsResult.Data)
        {
            errors.Add($"A project with the name '{project.Name}' already exists.");
        }

        return errors;
    }
}
