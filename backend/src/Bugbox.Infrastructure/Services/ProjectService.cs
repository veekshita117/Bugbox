using Bugbox.Core.DTOs.Projects;
using Bugbox.Core.Entities;
using Bugbox.Core.Enums;
using Bugbox.Core.Exceptions;
using Bugbox.Core.Interfaces;
using Bugbox.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Bugbox.Infrastructure.Services;

public class ProjectService : IProjectService
{
    private readonly BugboxDbContext _dbContext;

    public ProjectService(BugboxDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<ProjectDto>> GetUserProjectsAsync(Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var projects = await _dbContext.Projects
            .AsNoTracking()
            .Where(p => p.Members.Any(m => m.UserId == currentUserId))
            .Select(p => new ProjectDto(
                p.Id,
                p.Name,
                p.Key,
                p.Description,
                p.OwnerId,
                p.CreatedAt,
                p.Members.Where(m => m.UserId == currentUserId).Select(m => (ProjectRole?)m.Role).FirstOrDefault()
            ))
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken);

        return projects;
    }

    public async Task<ProjectDto> GetProjectByIdAsync(Guid projectId, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var project = await _dbContext.Projects
            .AsNoTracking()
            .Include(p => p.Members)
            .FirstOrDefaultAsync(p => p.Id == projectId, cancellationToken);

        if (project is null)
        {
            throw new NotFoundException(nameof(Project), projectId);
        }

        var member = project.Members.FirstOrDefault(m => m.UserId == currentUserId);
        if (member is null)
        {
            throw new ForbiddenException("You do not have access to this project.");
        }

        return new ProjectDto(
            project.Id,
            project.Name,
            project.Key,
            project.Description,
            project.OwnerId,
            project.CreatedAt,
            member.Role
        );
    }

    public async Task<ProjectDto> CreateProjectAsync(CreateProjectRequest request, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var normalizedKey = request.Key.Trim().ToUpperInvariant();

        var keyExists = await _dbContext.Projects
            .AnyAsync(p => p.Key == normalizedKey, cancellationToken);

        if (keyExists)
        {
            throw new ConflictException($"Project with key '{normalizedKey}' already exists.");
        }

        var project = new Project
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Key = normalizedKey,
            Description = request.Description?.Trim(),
            OwnerId = currentUserId,
            CreatedAt = DateTime.UtcNow
        };

        var ownerMembership = new ProjectMember
        {
            ProjectId = project.Id,
            UserId = currentUserId,
            Role = ProjectRole.Admin
        };

        project.Members.Add(ownerMembership);

        _dbContext.Projects.Add(project);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new ProjectDto(
            project.Id,
            project.Name,
            project.Key,
            project.Description,
            project.OwnerId,
            project.CreatedAt,
            ProjectRole.Admin
        );
    }

    public async Task<ProjectDto> UpdateProjectAsync(Guid projectId, UpdateProjectRequest request, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var project = await _dbContext.Projects
            .Include(p => p.Members)
            .FirstOrDefaultAsync(p => p.Id == projectId, cancellationToken);

        if (project is null)
        {
            throw new NotFoundException(nameof(Project), projectId);
        }

        var member = project.Members.FirstOrDefault(m => m.UserId == currentUserId);
        if (member is null || member.Role != ProjectRole.Admin)
        {
            throw new ForbiddenException("Only project admins can modify project details.");
        }

        project.Name = request.Name.Trim();
        project.Description = request.Description?.Trim();

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new ProjectDto(
            project.Id,
            project.Name,
            project.Key,
            project.Description,
            project.OwnerId,
            project.CreatedAt,
            member.Role
        );
    }

    public async Task DeleteProjectAsync(Guid projectId, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var project = await _dbContext.Projects
            .Include(p => p.Members)
            .FirstOrDefaultAsync(p => p.Id == projectId, cancellationToken);

        if (project is null)
        {
            throw new NotFoundException(nameof(Project), projectId);
        }

        var member = project.Members.FirstOrDefault(m => m.UserId == currentUserId);
        if (member is null || member.Role != ProjectRole.Admin)
        {
            throw new ForbiddenException("Only project admins can delete a project.");
        }

        _dbContext.Projects.Remove(project);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ProjectMemberDto>> GetMembersAsync(Guid projectId, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var isMember = await _dbContext.ProjectMembers
            .AnyAsync(pm => pm.ProjectId == projectId && pm.UserId == currentUserId, cancellationToken);

        if (!isMember)
        {
            var projectExists = await _dbContext.Projects.AnyAsync(p => p.Id == projectId, cancellationToken);
            if (!projectExists)
            {
                throw new NotFoundException(nameof(Project), projectId);
            }
            throw new ForbiddenException("You do not have access to this project.");
        }

        var members = await _dbContext.ProjectMembers
            .AsNoTracking()
            .Where(pm => pm.ProjectId == projectId)
            .Include(pm => pm.User)
            .Select(pm => new ProjectMemberDto(
                pm.UserId,
                pm.User.Name,
                pm.User.Email,
                pm.Role
            ))
            .ToListAsync(cancellationToken);

        return members;
    }

    public async Task<ProjectMemberDto> AddMemberAsync(Guid projectId, AddProjectMemberRequest request, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var currentMember = await _dbContext.ProjectMembers
            .FirstOrDefaultAsync(pm => pm.ProjectId == projectId && pm.UserId == currentUserId, cancellationToken);

        if (currentMember is null || currentMember.Role != ProjectRole.Admin)
        {
            var projectExists = await _dbContext.Projects.AnyAsync(p => p.Id == projectId, cancellationToken);
            if (!projectExists)
            {
                throw new NotFoundException(nameof(Project), projectId);
            }
            throw new ForbiddenException("Only project admins can add members.");
        }

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var targetUser = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail, cancellationToken);

        if (targetUser is null)
        {
            throw new NotFoundException($"User with email '{request.Email}' was not found.");
        }

        var existingMembership = await _dbContext.ProjectMembers
            .FirstOrDefaultAsync(pm => pm.ProjectId == projectId && pm.UserId == targetUser.Id, cancellationToken);

        if (existingMembership is not null)
        {
            throw new ConflictException($"User '{request.Email}' is already a member of this project.");
        }

        var newMember = new ProjectMember
        {
            ProjectId = projectId,
            UserId = targetUser.Id,
            Role = request.Role
        };

        _dbContext.ProjectMembers.Add(newMember);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new ProjectMemberDto(targetUser.Id, targetUser.Name, targetUser.Email, newMember.Role);
    }

    public async Task RemoveMemberAsync(Guid projectId, Guid memberUserId, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var currentMember = await _dbContext.ProjectMembers
            .FirstOrDefaultAsync(pm => pm.ProjectId == projectId && pm.UserId == currentUserId, cancellationToken);

        if (currentMember is null || currentMember.Role != ProjectRole.Admin)
        {
            throw new ForbiddenException("Only project admins can remove members.");
        }

        var project = await _dbContext.Projects
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == projectId, cancellationToken);

        if (project is not null && project.OwnerId == memberUserId)
        {
            throw new BadRequestException("Cannot remove the project owner from the project.");
        }

        var targetMember = await _dbContext.ProjectMembers
            .FirstOrDefaultAsync(pm => pm.ProjectId == projectId && pm.UserId == memberUserId, cancellationToken);

        if (targetMember is null)
        {
            throw new NotFoundException("Project member not found.");
        }

        _dbContext.ProjectMembers.Remove(targetMember);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
