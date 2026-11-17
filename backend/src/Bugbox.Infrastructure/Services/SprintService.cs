using Bugbox.Core.DTOs.Sprints;
using Bugbox.Core.Entities;
using Bugbox.Core.Enums;
using Bugbox.Core.Exceptions;
using Bugbox.Core.Interfaces;
using Bugbox.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Bugbox.Infrastructure.Services;

public class SprintService : ISprintService
{
    private readonly BugboxDbContext _dbContext;

    public SprintService(BugboxDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<SprintDto>> GetProjectSprintsAsync(Guid projectId, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        await EnsureProjectMemberAsync(projectId, currentUserId, cancellationToken);

        var sprints = await _dbContext.Sprints
            .AsNoTracking()
            .Where(s => s.ProjectId == projectId)
            .OrderBy(s => s.Status == SprintStatus.Active ? 0 : s.Status == SprintStatus.Planned ? 1 : 2)
            .ThenBy(s => s.StartDate)
            .Include(s => s.Issues)
            .Select(s => new SprintDto(
                s.Id,
                s.ProjectId,
                s.Name,
                s.Goal,
                s.StartDate,
                s.EndDate,
                s.Status,
                s.Issues.Count,
                s.Issues.Count(i => i.Status == IssueStatus.Done)
            ))
            .ToListAsync(cancellationToken);

        return sprints;
    }

    public async Task<SprintDto> GetSprintByIdAsync(Guid sprintId, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var sprint = await _dbContext.Sprints
            .AsNoTracking()
            .Include(s => s.Issues)
            .FirstOrDefaultAsync(s => s.Id == sprintId, cancellationToken);

        if (sprint is null)
        {
            throw new NotFoundException(nameof(Sprint), sprintId);
        }

        await EnsureProjectMemberAsync(sprint.ProjectId, currentUserId, cancellationToken);

        return new SprintDto(
            sprint.Id,
            sprint.ProjectId,
            sprint.Name,
            sprint.Goal,
            sprint.StartDate,
            sprint.EndDate,
            sprint.Status,
            sprint.Issues.Count,
            sprint.Issues.Count(i => i.Status == IssueStatus.Done)
        );
    }

    public async Task<SprintDto> CreateSprintAsync(Guid projectId, CreateSprintRequest request, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        await EnsureProjectMemberAsync(projectId, currentUserId, cancellationToken);

        var sprint = new Sprint
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            Name = request.Name.Trim(),
            Goal = request.Goal?.Trim(),
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Status = SprintStatus.Planned
        };

        _dbContext.Sprints.Add(sprint);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new SprintDto(
            sprint.Id,
            sprint.ProjectId,
            sprint.Name,
            sprint.Goal,
            sprint.StartDate,
            sprint.EndDate,
            sprint.Status,
            0,
            0
        );
    }

    public async Task<SprintDto> UpdateSprintAsync(Guid sprintId, UpdateSprintRequest request, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var sprint = await _dbContext.Sprints
            .Include(s => s.Issues)
            .FirstOrDefaultAsync(s => s.Id == sprintId, cancellationToken);

        if (sprint is null)
        {
            throw new NotFoundException(nameof(Sprint), sprintId);
        }

        await EnsureProjectMemberAsync(sprint.ProjectId, currentUserId, cancellationToken);

        if (sprint.Status == SprintStatus.Completed)
        {
            throw new BadRequestException("Cannot modify a completed sprint.");
        }

        sprint.Name = request.Name.Trim();
        sprint.Goal = request.Goal?.Trim();
        sprint.StartDate = request.StartDate;
        sprint.EndDate = request.EndDate;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new SprintDto(
            sprint.Id,
            sprint.ProjectId,
            sprint.Name,
            sprint.Goal,
            sprint.StartDate,
            sprint.EndDate,
            sprint.Status,
            sprint.Issues.Count,
            sprint.Issues.Count(i => i.Status == IssueStatus.Done)
        );
    }

    public async Task DeleteSprintAsync(Guid sprintId, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var sprint = await _dbContext.Sprints
            .Include(s => s.Issues)
            .FirstOrDefaultAsync(s => s.Id == sprintId, cancellationToken);

        if (sprint is null)
        {
            throw new NotFoundException(nameof(Sprint), sprintId);
        }

        await EnsureProjectMemberAsync(sprint.ProjectId, currentUserId, cancellationToken);

        // Per requirement: DELETE for Planned sprints only
        if (sprint.Status != SprintStatus.Planned)
        {
            throw new BadRequestException("Only planned sprints can be deleted.");
        }

        // Return issues attached to this sprint back to the backlog
        foreach (var issue in sprint.Issues)
        {
            issue.SprintId = null;
        }

        _dbContext.Sprints.Remove(sprint);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<SprintDto> StartSprintAsync(Guid sprintId, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var sprint = await _dbContext.Sprints
            .Include(s => s.Issues)
            .FirstOrDefaultAsync(s => s.Id == sprintId, cancellationToken);

        if (sprint is null)
        {
            throw new NotFoundException(nameof(Sprint), sprintId);
        }

        await EnsureProjectMemberAsync(sprint.ProjectId, currentUserId, cancellationToken);

        if (sprint.Status != SprintStatus.Planned)
        {
            throw new BadRequestException("Only planned sprints can be started.");
        }

        // Per requirement: Only one Active sprint per project at a time
        var hasActiveSprint = await _dbContext.Sprints
            .AnyAsync(s => s.ProjectId == sprint.ProjectId && s.Status == SprintStatus.Active && s.Id != sprintId, cancellationToken);

        if (hasActiveSprint)
        {
            throw new ConflictException("Another sprint is currently active in this project. Complete it before starting a new one.");
        }

        sprint.Status = SprintStatus.Active;
        sprint.StartDate ??= DateTime.UtcNow;
        sprint.EndDate ??= sprint.StartDate.Value.AddDays(14);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new SprintDto(
            sprint.Id,
            sprint.ProjectId,
            sprint.Name,
            sprint.Goal,
            sprint.StartDate,
            sprint.EndDate,
            sprint.Status,
            sprint.Issues.Count,
            sprint.Issues.Count(i => i.Status == IssueStatus.Done)
        );
    }

    public async Task<SprintDto> CompleteSprintAsync(Guid sprintId, CompleteSprintRequest request, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var sprint = await _dbContext.Sprints
            .Include(s => s.Issues)
            .FirstOrDefaultAsync(s => s.Id == sprintId, cancellationToken);

        if (sprint is null)
        {
            throw new NotFoundException(nameof(Sprint), sprintId);
        }

        await EnsureProjectMemberAsync(sprint.ProjectId, currentUserId, cancellationToken);

        if (sprint.Status != SprintStatus.Active)
        {
            throw new BadRequestException("Only active sprints can be completed.");
        }

        Sprint? targetSprint = null;
        if (request.TargetSprintId.HasValue)
        {
            targetSprint = await _dbContext.Sprints
                .FirstOrDefaultAsync(s => s.Id == request.TargetSprintId.Value && s.ProjectId == sprint.ProjectId, cancellationToken);

            if (targetSprint is null)
            {
                throw new NotFoundException("Target sprint was not found in this project.");
            }

            if (targetSprint.Status == SprintStatus.Completed)
            {
                throw new BadRequestException("Cannot move unfinished issues to a completed sprint.");
            }
        }

        sprint.Status = SprintStatus.Completed;

        // Unfinished issues go back to backlog (null) or move to targetSprint
        var unfinishedIssues = sprint.Issues.Where(i => i.Status != IssueStatus.Done).ToList();
        foreach (var issue in unfinishedIssues)
        {
            issue.SprintId = targetSprint?.Id;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new SprintDto(
            sprint.Id,
            sprint.ProjectId,
            sprint.Name,
            sprint.Goal,
            sprint.StartDate,
            sprint.EndDate,
            sprint.Status,
            sprint.Issues.Count,
            sprint.Issues.Count(i => i.Status == IssueStatus.Done)
        );
    }

    public async Task AddIssuesToSprintAsync(Guid sprintId, AssignSprintIssuesRequest request, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var sprint = await _dbContext.Sprints
            .FirstOrDefaultAsync(s => s.Id == sprintId, cancellationToken);

        if (sprint is null)
        {
            throw new NotFoundException(nameof(Sprint), sprintId);
        }

        await EnsureProjectMemberAsync(sprint.ProjectId, currentUserId, cancellationToken);

        if (sprint.Status == SprintStatus.Completed)
        {
            throw new BadRequestException("Cannot add issues to a completed sprint.");
        }

        var issues = await _dbContext.Issues
            .Where(i => i.ProjectId == sprint.ProjectId && request.IssueIds.Contains(i.Id))
            .ToListAsync(cancellationToken);

        foreach (var issue in issues)
        {
            issue.SprintId = sprintId;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveIssueFromSprintAsync(Guid sprintId, Guid issueId, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var sprint = await _dbContext.Sprints
            .FirstOrDefaultAsync(s => s.Id == sprintId, cancellationToken);

        if (sprint is null)
        {
            throw new NotFoundException(nameof(Sprint), sprintId);
        }

        await EnsureProjectMemberAsync(sprint.ProjectId, currentUserId, cancellationToken);

        var issue = await _dbContext.Issues
            .FirstOrDefaultAsync(i => i.Id == issueId && i.ProjectId == sprint.ProjectId, cancellationToken);

        if (issue is not null && issue.SprintId == sprintId)
        {
            issue.SprintId = null; // Back to backlog
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<IReadOnlyList<BurndownPointDto>> GetBurndownAsync(Guid sprintId, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var sprint = await _dbContext.Sprints
            .AsNoTracking()
            .Include(s => s.Issues)
            .FirstOrDefaultAsync(s => s.Id == sprintId, cancellationToken);

        if (sprint is null)
        {
            throw new NotFoundException(nameof(Sprint), sprintId);
        }

        await EnsureProjectMemberAsync(sprint.ProjectId, currentUserId, cancellationToken);

        /*
         * BURNDOWN CALCULATION (LINQ / In-Memory projection):
         * 1. Window bounds: Start from StartDate.Date to EndDate.Date (or defaults if unassigned).
         * 2. Total issues count (T): All issues in the sprint snapshot.
         * 3. Ideal line: Linear descent from T at Day 0 down to 0 at Day N (totalDays).
         *    ideal = Math.Round(T - (T * ((double)dayIndex / totalDays)), 2)
         * 4. Remaining issues: Total issues minus those that have CompletedAt on or before that day
         *    AND whose current status is Done. If an issue was finished and then re-opened,
         *    its Status is no longer Done and CompletedAt was cleared, so it correctly stays
         *    in the remaining count.
         */
        var startDate = (sprint.StartDate ?? DateTime.UtcNow).Date;
        var endDate = (sprint.EndDate ?? startDate.AddDays(14)).Date;

        if (endDate < startDate)
        {
            endDate = startDate;
        }

        var totalDays = (endDate - startDate).Days;
        var totalIssues = sprint.Issues.Count;
        var points = new List<BurndownPointDto>();

        for (var dayIndex = 0; dayIndex <= totalDays; dayIndex++)
        {
            var currentDay = startDate.AddDays(dayIndex);

            // Compute ideal remaining issues along the linear descent line
            var ideal = totalDays == 0
                ? 0.0
                : Math.Round(Math.Max(0.0, totalIssues - (totalIssues * ((double)dayIndex / totalDays))), 2);

            // Issues completed on or before currentDay
            var completedCount = sprint.Issues.Count(i =>
                i.Status == IssueStatus.Done &&
                i.CompletedAt.HasValue &&
                i.CompletedAt.Value.Date <= currentDay);

            var remaining = Math.Max(0, totalIssues - completedCount);

            points.Add(new BurndownPointDto(
                currentDay.ToString("yyyy-MM-dd"),
                remaining,
                ideal
            ));
        }

        return points;
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
}
