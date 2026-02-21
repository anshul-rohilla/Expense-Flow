using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Expense_Flow.Data;
using Expense_Flow.Models;
using Expense_Flow.Helpers;

namespace Expense_Flow.Services;

public interface IProjectGroupService
{
    Task<ServiceResult<IEnumerable<ProjectGroup>>> GetAllProjectGroupsAsync();
    Task<ServiceResult<ProjectGroup>> GetProjectGroupByIdAsync(int id);
    Task<ServiceResult<ProjectGroup>> CreateProjectGroupAsync(ProjectGroup projectGroup);
    Task<ServiceResult<ProjectGroup>> UpdateProjectGroupAsync(ProjectGroup projectGroup);
    Task<ServiceResult<bool>> DeleteProjectGroupAsync(int id);
    Task<ServiceResult<bool>> AddProjectToGroupAsync(int projectGroupId, int projectId);
    Task<ServiceResult<bool>> RemoveProjectFromGroupAsync(int projectGroupId, int projectId);
    Task<ServiceResult<IEnumerable<Project>>> GetProjectsInGroupAsync(int projectGroupId);
    Task<ServiceResult<bool>> ProjectGroupExistsAsync(string name, int? excludeId = null);
}

public class ProjectGroupService : IProjectGroupService
{
    private readonly IRepository<ProjectGroup> _projectGroupRepository;
    private readonly IRepository<ProjectGroupMapping> _mappingRepository;
    private readonly IRepository<Project> _projectRepository;
    private readonly IUserService _userService;

    public ProjectGroupService(
        IRepository<ProjectGroup> projectGroupRepository,
        IRepository<ProjectGroupMapping> mappingRepository,
        IRepository<Project> projectRepository,
        IUserService userService)
    {
        _projectGroupRepository = projectGroupRepository;
        _mappingRepository = mappingRepository;
        _projectRepository = projectRepository;
        _userService = userService;
    }

    public async Task<ServiceResult<IEnumerable<ProjectGroup>>> GetAllProjectGroupsAsync()
    {
        try
        {
            var projectGroups = await _projectGroupRepository.GetAllAsync();
            return ServiceResult<IEnumerable<ProjectGroup>>.SuccessResult(projectGroups.OrderBy(pg => pg.Name));
        }
        catch (Exception ex)
        {
            return ServiceResult<IEnumerable<ProjectGroup>>.FailureResult($"Error retrieving project groups: {ex.Message}");
        }
    }

    public async Task<ServiceResult<ProjectGroup>> GetProjectGroupByIdAsync(int id)
    {
        try
        {
            var projectGroup = await _projectGroupRepository.GetByIdAsync(id);
            if (projectGroup == null)
            {
                return ServiceResult<ProjectGroup>.FailureResult("Project group not found.");
            }
            return ServiceResult<ProjectGroup>.SuccessResult(projectGroup);
        }
        catch (Exception ex)
        {
            return ServiceResult<ProjectGroup>.FailureResult($"Error retrieving project group: {ex.Message}");
        }
    }

    public async Task<ServiceResult<ProjectGroup>> CreateProjectGroupAsync(ProjectGroup projectGroup)
    {
        try
        {
            var validationErrors = await ValidateProjectGroupAsync(projectGroup);
            if (validationErrors.Any())
            {
                return ServiceResult<ProjectGroup>.FailureResult(validationErrors);
            }

            projectGroup.CreatedAt = DateTime.Now;
            projectGroup.CreatedBy = _userService.GetCurrentUsername();
            projectGroup.ModifiedAt = null;
            projectGroup.ModifiedBy = null;

            var createdProjectGroup = await _projectGroupRepository.AddAsync(projectGroup);
            return ServiceResult<ProjectGroup>.SuccessResult(createdProjectGroup, "Project group created successfully.");
        }
        catch (Exception ex)
        {
            return ServiceResult<ProjectGroup>.FailureResult($"Error creating project group: {ex.Message}");
        }
    }

    public async Task<ServiceResult<ProjectGroup>> UpdateProjectGroupAsync(ProjectGroup projectGroup)
    {
        try
        {
            var existingProjectGroup = await _projectGroupRepository.GetByIdAsync(projectGroup.Id);
            if (existingProjectGroup == null)
            {
                return ServiceResult<ProjectGroup>.FailureResult("Project group not found.");
            }

            var validationErrors = await ValidateProjectGroupAsync(projectGroup, projectGroup.Id);
            if (validationErrors.Any())
            {
                return ServiceResult<ProjectGroup>.FailureResult(validationErrors);
            }

            existingProjectGroup.Name = projectGroup.Name;
            existingProjectGroup.Description = projectGroup.Description;
            existingProjectGroup.ModifiedAt = DateTime.Now;
            existingProjectGroup.ModifiedBy = _userService.GetCurrentUsername();

            var updatedProjectGroup = await _projectGroupRepository.UpdateAsync(existingProjectGroup);
            return ServiceResult<ProjectGroup>.SuccessResult(updatedProjectGroup, "Project group updated successfully.");
        }
        catch (Exception ex)
        {
            return ServiceResult<ProjectGroup>.FailureResult($"Error updating project group: {ex.Message}");
        }
    }

    public async Task<ServiceResult<bool>> DeleteProjectGroupAsync(int id)
    {
        try
        {
            var projectGroup = await _projectGroupRepository.GetByIdAsync(id);
            if (projectGroup == null)
            {
                return ServiceResult<bool>.FailureResult("Project group not found.");
            }

            await _projectGroupRepository.DeleteAsync(projectGroup);
            return ServiceResult<bool>.SuccessResult(true, "Project group deleted successfully.");
        }
        catch (Exception ex)
        {
            return ServiceResult<bool>.FailureResult($"Error deleting project group: {ex.Message}");
        }
    }

    public async Task<ServiceResult<bool>> AddProjectToGroupAsync(int projectGroupId, int projectId)
    {
        try
        {
            var projectGroup = await _projectGroupRepository.GetByIdAsync(projectGroupId);
            if (projectGroup == null)
            {
                return ServiceResult<bool>.FailureResult("Project group not found.");
            }

            var project = await _projectRepository.GetByIdAsync(projectId);
            if (project == null)
            {
                return ServiceResult<bool>.FailureResult("Project not found.");
            }

            var exists = await _mappingRepository.ExistsAsync(m =>
                m.ProjectGroupId == projectGroupId && m.ProjectId == projectId);

            if (exists)
            {
                return ServiceResult<bool>.FailureResult("Project is already in this group.");
            }

            var mapping = new ProjectGroupMapping
            {
                ProjectGroupId = projectGroupId,
                ProjectId = projectId,
                CreatedAt = DateTime.Now,
                CreatedBy = _userService.GetCurrentUsername()
            };

            await _mappingRepository.AddAsync(mapping);
            return ServiceResult<bool>.SuccessResult(true, "Project added to group successfully.");
        }
        catch (Exception ex)
        {
            return ServiceResult<bool>.FailureResult($"Error adding project to group: {ex.Message}");
        }
    }

    public async Task<ServiceResult<bool>> RemoveProjectFromGroupAsync(int projectGroupId, int projectId)
    {
        try
        {
            var mappings = await _mappingRepository.FindAsync(m =>
                m.ProjectGroupId == projectGroupId && m.ProjectId == projectId);

            var mapping = mappings.FirstOrDefault();
            if (mapping == null)
            {
                return ServiceResult<bool>.FailureResult("Project is not in this group.");
            }

            await _mappingRepository.DeleteAsync(mapping);
            return ServiceResult<bool>.SuccessResult(true, "Project removed from group successfully.");
        }
        catch (Exception ex)
        {
            return ServiceResult<bool>.FailureResult($"Error removing project from group: {ex.Message}");
        }
    }

    public async Task<ServiceResult<IEnumerable<Project>>> GetProjectsInGroupAsync(int projectGroupId)
    {
        try
        {
            var mappings = await _mappingRepository.FindAsync(m => m.ProjectGroupId == projectGroupId);
            var projectIds = mappings.Select(m => m.ProjectId).ToList();

            var projects = await _projectRepository.FindWithIncludeAsync(
                p => projectIds.Contains(p.Id),
                p => p.Expenses);
            return ServiceResult<IEnumerable<Project>>.SuccessResult(projects.OrderBy(p => p.Name));
        }
        catch (Exception ex)
        {
            return ServiceResult<IEnumerable<Project>>.FailureResult($"Error retrieving projects in group: {ex.Message}");
        }
    }

    public async Task<ServiceResult<bool>> ProjectGroupExistsAsync(string name, int? excludeId = null)
    {
        try
        {
            var exists = await _projectGroupRepository.ExistsAsync(pg =>
                pg.Name.ToLower() == name.ToLower() &&
                (!excludeId.HasValue || pg.Id != excludeId.Value));

            return ServiceResult<bool>.SuccessResult(exists);
        }
        catch (Exception ex)
        {
            return ServiceResult<bool>.FailureResult($"Error checking project group existence: {ex.Message}");
        }
    }

    private async Task<List<string>> ValidateProjectGroupAsync(ProjectGroup projectGroup, int? excludeId = null)
    {
        var errors = new List<string>();

        errors.AddRange(ValidationHelper.ValidateRequired(projectGroup.Name, "Name"));
        errors.AddRange(ValidationHelper.ValidateMaxLength(projectGroup.Name, 200, "Name"));
        errors.AddRange(ValidationHelper.ValidateMaxLength(projectGroup.Description, 1000, "Description"));

        var existsResult = await ProjectGroupExistsAsync(projectGroup.Name, excludeId);
        if (existsResult.Success && existsResult.Data)
        {
            errors.Add($"A project group with the name '{projectGroup.Name}' already exists.");
        }

        return errors;
    }
}
