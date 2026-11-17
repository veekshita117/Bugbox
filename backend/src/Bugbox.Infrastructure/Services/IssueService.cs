using System.Data;
using Bugbox.Core.DTOs.Auth;
using Bugbox.Core.DTOs.Common;
using Bugbox.Core.DTOs.Issues;
using Bugbox.Core.DTOs.Labels;
using Bugbox.Core.Entities;
using Bugbox.Core.Enums;
using Bugbox.Core.Exceptions;
using Bugbox.Core.Interfaces;
using Bugbox.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Bugbox.Infrastructure.Services;

public class IssueService : IIssueService
{
    private readonly BugboxDbContext _dbContext;

    public IssueService(BugboxDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PagedResult<IssueDto>> GetIssuesAsync(
        Guid projectId,
        IssueFilterRequest filter,
        Guid currentUserId,
        CancellationToken cancellationToken = default)
    {
        await EnsureProjectMemberAsync(projectId, currentUserId, cancellationToken);

        var query = _dbContext.Issues
            .AsNoTracking()
            .Where(i => i.ProjectId == projectId);

        if (filter.Status.HasValue)
        {
            query = query.Where(i => i.Status == filter.Status.Value);
        }

        if (filter.AssigneeId.HasValue)
        {
            query = query.Where(i => i.AssigneeId == filter.AssigneeId.Value);
        }

        if (filter.SprintId.HasValue)
        {
            query = query.Where(i => i.SprintId == filter.SprintId.Value);
        }

        if (filter.Type.HasValue)
        {
            query = query.Where(i => i.Type == filter.Type.Value);
        }

        if (filter.Priority.HasValue)
        {
            query = query.Where(i => i.Priority == filter.Priority.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.Trim().ToLower();
            query = query.Where(i => i.Title.ToLower().Contains(search));
        }

        if (filter.LabelId.HasValue)
        {
            query = query.Where(i => i.IssueLabels.Any(il => il.LabelId == filter.LabelId.Value));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var page = filter.Page < 1 ? 1 : filter.Page;
        var pageSize = filter.PageSize < 1 ? 20 : (filter.PageSize > 100 ? 100 : filter.PageSize);

        var issueEntities = await query
            .OrderBy(i => i.BoardOrder)
            .ThenByDescending(i => i.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Include(i => i.Project)
            .Include(i => i.Assignee)
            .Include(i => i.Reporter)
            .Include(i => i.IssueLabels).ThenInclude(il => il.Label)
            .Include(i => i.Comments)
            .ToListAsync(cancellationToken);

        var issues = issueEntities.Select(MapToDto).ToList();

        return PagedResult<IssueDto>.Create(issues, totalCount, page, pageSize);
    }

    public async Task<IssueDto> GetIssueByIdAsync(Guid issueId, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var issue = await _dbContext.Issues
            .AsNoTracking()
            .Include(i => i.Project)
            .Include(i => i.Assignee)
            .Include(i => i.Reporter)
            .Include(i => i.IssueLabels).ThenInclude(il => il.Label)
            .Include(i => i.Comments)
            .FirstOrDefaultAsync(i => i.Id == issueId, cancellationToken);

        if (issue is null)
        {
            throw new NotFoundException(nameof(Issue), issueId);
        }

        await EnsureProjectMemberAsync(issue.ProjectId, currentUserId, cancellationToken);

        return MapToDto(issue);
    }

    public async Task<IssueDto> CreateIssueAsync(
        Guid projectId,
        CreateIssueRequest request,
        Guid currentUserId,
        CancellationToken cancellationToken = default)
    {
        await EnsureProjectMemberAsync(projectId, currentUserId, cancellationToken);

        var project = await _dbContext.Projects
            .FirstOrDefaultAsync(p => p.Id == projectId, cancellationToken);

        if (project is null)
        {
            throw new NotFoundException(nameof(Project), projectId);
        }

        // Validate Assignee belongs to project if specified
        if (request.AssigneeId.HasValue)
        {
            var isAssigneeMember = await _dbContext.ProjectMembers
                .AnyAsync(pm => pm.ProjectId == projectId && pm.UserId == request.AssigneeId.Value, cancellationToken);

            if (!isAssigneeMember)
            {
                throw new BadRequestException("Assignee must be a member of the project.");
            }
        }

        // Validate Sprint belongs to project if specified
        if (request.SprintId.HasValue)
        {
            var sprintValid = await _dbContext.Sprints
                .AnyAsync(s => s.ProjectId == projectId && s.Id == request.SprintId.Value, cancellationToken);

            if (!sprintValid)
            {
                throw new BadRequestException("Selected sprint does not belong to this project.");
            }
        }

        var isRelational = _dbContext.Database.IsRelational();
        await using var transaction = isRelational
            ? await _dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken)
            : null;

        // Safely determine next sequential per-project number
        var maxNumber = await _dbContext.Issues
            .Where(i => i.ProjectId == projectId)
            .MaxAsync(i => (int?)i.Number, cancellationToken) ?? 0;

        var nextNumber = maxNumber + 1;

        // Calculate next BoardOrder in this status column
        var maxOrder = await _dbContext.Issues
            .Where(i => i.ProjectId == projectId && i.Status == request.Status)
            .MaxAsync(i => (int?)i.BoardOrder, cancellationToken) ?? -1;

        var boardOrder = maxOrder + 1;
        var now = DateTime.UtcNow;

        var issue = new Issue
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            Project = project,
            SprintId = request.SprintId,
            Number = nextNumber,
            Title = request.Title.Trim(),
            Description = request.Description?.Trim(),
            Type = request.Type,
            Priority = request.Priority,
            Status = request.Status,
            AssigneeId = request.AssigneeId,
            ReporterId = currentUserId,
            BoardOrder = boardOrder,
            CreatedAt = now,
            UpdatedAt = now,
            CompletedAt = request.Status == IssueStatus.Done ? now : null
        };

        if (request.LabelIds is not null && request.LabelIds.Count > 0)
        {
            var validLabels = await _dbContext.Labels
                .Where(l => l.ProjectId == projectId && request.LabelIds.Contains(l.Id))
                .ToListAsync(cancellationToken);

            foreach (var label in validLabels)
            {
                issue.IssueLabels.Add(new IssueLabel
                {
                    IssueId = issue.Id,
                    LabelId = label.Id
                });
            }
        }

        _dbContext.Issues.Add(issue);
        await _dbContext.SaveChangesAsync(cancellationToken);

        if (transaction is not null)
        {
            await transaction.CommitAsync(cancellationToken);
        }

        // Reload navigation references for DTO
        await _dbContext.Entry(issue).Reference(i => i.Reporter).LoadAsync(cancellationToken);
        if (issue.AssigneeId.HasValue)
        {
            await _dbContext.Entry(issue).Reference(i => i.Assignee).LoadAsync(cancellationToken);
        }
        await _dbContext.Entry(issue).Collection(i => i.IssueLabels).Query().Include(il => il.Label).LoadAsync(cancellationToken);

        return MapToDto(issue);
    }

    public async Task<IssueDto> UpdateIssueAsync(
        Guid issueId,
        UpdateIssueRequest request,
        Guid currentUserId,
        CancellationToken cancellationToken = default)
    {
        var issue = await _dbContext.Issues
            .Include(i => i.Project)
            .Include(i => i.Assignee)
            .Include(i => i.Reporter)
            .Include(i => i.IssueLabels).ThenInclude(il => il.Label)
            .Include(i => i.Comments)
            .FirstOrDefaultAsync(i => i.Id == issueId, cancellationToken);

        if (issue is null)
        {
            throw new NotFoundException(nameof(Issue), issueId);
        }

        await EnsureProjectMemberAsync(issue.ProjectId, currentUserId, cancellationToken);

        if (request.AssigneeId.HasValue)
        {
            var isAssigneeMember = await _dbContext.ProjectMembers
                .AnyAsync(pm => pm.ProjectId == issue.ProjectId && pm.UserId == request.AssigneeId.Value, cancellationToken);

            if (!isAssigneeMember)
            {
                throw new BadRequestException("Assignee must be a member of the project.");
            }
        }

        if (request.SprintId.HasValue)
        {
            var sprintValid = await _dbContext.Sprints
                .AnyAsync(s => s.ProjectId == issue.ProjectId && s.Id == request.SprintId.Value, cancellationToken);

            if (!sprintValid)
            {
                throw new BadRequestException("Selected sprint does not belong to this project.");
            }
        }

        var oldStatus = issue.Status;
        var newStatus = request.Status;

        issue.Title = request.Title.Trim();
        issue.Description = request.Description?.Trim();
        issue.Type = request.Type;
        issue.Priority = request.Priority;
        issue.Status = newStatus;
        issue.AssigneeId = request.AssigneeId;
        issue.SprintId = request.SprintId;
        issue.UpdatedAt = DateTime.UtcNow;

        if (oldStatus != IssueStatus.Done && newStatus == IssueStatus.Done)
        {
            issue.CompletedAt = DateTime.UtcNow;
        }
        else if (oldStatus == IssueStatus.Done && newStatus != IssueStatus.Done)
        {
            issue.CompletedAt = null;
        }

        // Update Labels
        if (request.LabelIds is not null)
        {
            issue.IssueLabels.Clear();
            var validLabels = await _dbContext.Labels
                .Where(l => l.ProjectId == issue.ProjectId && request.LabelIds.Contains(l.Id))
                .ToListAsync(cancellationToken);

            foreach (var label in validLabels)
            {
                issue.IssueLabels.Add(new IssueLabel
                {
                    IssueId = issue.Id,
                    LabelId = label.Id
                });
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        // Reload references
        if (issue.AssigneeId.HasValue)
        {
            await _dbContext.Entry(issue).Reference(i => i.Assignee).LoadAsync(cancellationToken);
        }
        else
        {
            issue.Assignee = null;
        }

        return MapToDto(issue);
    }

    public async Task DeleteIssueAsync(Guid issueId, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var issue = await _dbContext.Issues
            .FirstOrDefaultAsync(i => i.Id == issueId, cancellationToken);

        if (issue is null)
        {
            throw new NotFoundException(nameof(Issue), issueId);
        }

        var member = await _dbContext.ProjectMembers
            .FirstOrDefaultAsync(pm => pm.ProjectId == issue.ProjectId && pm.UserId == currentUserId, cancellationToken);

        if (member is null)
        {
            throw new ForbiddenException("You do not have access to this project.");
        }

        // Admin or Reporter can delete issue
        if (member.Role != ProjectRole.Admin && issue.ReporterId != currentUserId)
        {
            throw new ForbiddenException("Only project admins or the issue reporter can delete this issue.");
        }

        _dbContext.Issues.Remove(issue);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IssueDto> UpdateStatusAsync(
        Guid issueId,
        UpdateIssueStatusRequest request,
        Guid currentUserId,
        CancellationToken cancellationToken = default)
    {
        var issue = await _dbContext.Issues
            .Include(i => i.Project)
            .Include(i => i.Assignee)
            .Include(i => i.Reporter)
            .Include(i => i.IssueLabels).ThenInclude(il => il.Label)
            .Include(i => i.Comments)
            .FirstOrDefaultAsync(i => i.Id == issueId, cancellationToken);

        if (issue is null)
        {
            throw new NotFoundException(nameof(Issue), issueId);
        }

        await EnsureProjectMemberAsync(issue.ProjectId, currentUserId, cancellationToken);

        var oldStatus = issue.Status;
        var newStatus = request.Status;

        if (oldStatus != newStatus)
        {
            issue.Status = newStatus;
            issue.UpdatedAt = DateTime.UtcNow;

            if (newStatus == IssueStatus.Done)
            {
                issue.CompletedAt = DateTime.UtcNow;
            }
            else if (oldStatus == IssueStatus.Done)
            {
                issue.CompletedAt = null;
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return MapToDto(issue);
    }

    public async Task<IssueDto> MoveIssueAsync(
        Guid issueId,
        MoveIssueRequest request,
        Guid currentUserId,
        CancellationToken cancellationToken = default)
    {
        var issue = await _dbContext.Issues
            .Include(i => i.Project)
            .Include(i => i.Assignee)
            .Include(i => i.Reporter)
            .Include(i => i.IssueLabels).ThenInclude(il => il.Label)
            .Include(i => i.Comments)
            .FirstOrDefaultAsync(i => i.Id == issueId, cancellationToken);

        if (issue is null)
        {
            throw new NotFoundException(nameof(Issue), issueId);
        }

        await EnsureProjectMemberAsync(issue.ProjectId, currentUserId, cancellationToken);

        var oldStatus = issue.Status;
        var newStatus = request.Status;

        issue.Status = newStatus;
        issue.BoardOrder = request.BoardOrder;
        issue.UpdatedAt = DateTime.UtcNow;

        if (oldStatus != IssueStatus.Done && newStatus == IssueStatus.Done)
        {
            issue.CompletedAt = DateTime.UtcNow;
        }
        else if (oldStatus == IssueStatus.Done && newStatus != IssueStatus.Done)
        {
            issue.CompletedAt = null;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return MapToDto(issue);
    }

    public async Task<IssueDto> AddLabelToIssueAsync(
        Guid issueId,
        Guid labelId,
        Guid currentUserId,
        CancellationToken cancellationToken = default)
    {
        var issue = await _dbContext.Issues
            .Include(i => i.Project)
            .Include(i => i.Assignee)
            .Include(i => i.Reporter)
            .Include(i => i.IssueLabels).ThenInclude(il => il.Label)
            .Include(i => i.Comments)
            .FirstOrDefaultAsync(i => i.Id == issueId, cancellationToken);

        if (issue is null)
        {
            throw new NotFoundException(nameof(Issue), issueId);
        }

        await EnsureProjectMemberAsync(issue.ProjectId, currentUserId, cancellationToken);

        var label = await _dbContext.Labels
            .FirstOrDefaultAsync(l => l.Id == labelId && l.ProjectId == issue.ProjectId, cancellationToken);

        if (label is null)
        {
            throw new NotFoundException("Label not found in this project.");
        }

        var alreadyHasLabel = issue.IssueLabels.Any(il => il.LabelId == labelId);
        if (!alreadyHasLabel)
        {
            issue.IssueLabels.Add(new IssueLabel
            {
                IssueId = issue.Id,
                LabelId = label.Id,
                Label = label
            });
            issue.UpdatedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return MapToDto(issue);
    }

    public async Task<IssueDto> RemoveLabelFromIssueAsync(
        Guid issueId,
        Guid labelId,
        Guid currentUserId,
        CancellationToken cancellationToken = default)
    {
        var issue = await _dbContext.Issues
            .Include(i => i.Project)
            .Include(i => i.Assignee)
            .Include(i => i.Reporter)
            .Include(i => i.IssueLabels).ThenInclude(il => il.Label)
            .Include(i => i.Comments)
            .FirstOrDefaultAsync(i => i.Id == issueId, cancellationToken);

        if (issue is null)
        {
            throw new NotFoundException(nameof(Issue), issueId);
        }

        await EnsureProjectMemberAsync(issue.ProjectId, currentUserId, cancellationToken);

        var existing = issue.IssueLabels.FirstOrDefault(il => il.LabelId == labelId);
        if (existing is not null)
        {
            issue.IssueLabels.Remove(existing);
            issue.UpdatedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return MapToDto(issue);
    }

    private async Task EnsureProjectMemberAsync(Guid projectId, Guid currentUserId, CancellationToken cancellationToken)
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
    }

    private static IssueDto MapToDto(Issue i)
    {
        var projectKey = i.Project?.Key ?? string.Empty;
        var issueKey = $"{projectKey}-{i.Number}";

        var labels = i.IssueLabels?
            .Where(il => il.Label != null)
            .Select(il => new LabelDto(il.Label.Id, il.Label.ProjectId, il.Label.Name, il.Label.Color))
            .ToList() ?? new List<LabelDto>();

        var assignee = i.Assignee is not null
            ? new UserDto(i.Assignee.Id, i.Assignee.Name, i.Assignee.Email, i.Assignee.CreatedAt)
            : null;

        var reporter = i.Reporter is not null
            ? new UserDto(i.Reporter.Id, i.Reporter.Name, i.Reporter.Email, i.Reporter.CreatedAt)
            : new UserDto(i.ReporterId, string.Empty, string.Empty, DateTime.MinValue);

        return new IssueDto(
            i.Id,
            i.ProjectId,
            projectKey,
            issueKey,
            i.Number,
            i.Title,
            i.Description,
            i.Type,
            i.Priority,
            i.Status,
            i.BoardOrder,
            assignee,
            reporter,
            i.SprintId,
            labels,
            i.CreatedAt,
            i.UpdatedAt,
            i.CompletedAt,
            i.Comments?.Count ?? 0
        );
    }
}
