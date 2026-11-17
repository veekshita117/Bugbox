using Bugbox.Core.DTOs.Labels;
using Bugbox.Core.Entities;
using Bugbox.Core.Exceptions;
using Bugbox.Core.Interfaces;
using Bugbox.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Bugbox.Infrastructure.Services;

public class LabelService : ILabelService
{
    private readonly BugboxDbContext _dbContext;

    public LabelService(BugboxDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<LabelDto>> GetLabelsAsync(Guid projectId, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        await EnsureProjectMemberAsync(projectId, currentUserId, cancellationToken);

        return await _dbContext.Labels
            .AsNoTracking()
            .Where(l => l.ProjectId == projectId)
            .OrderBy(l => l.Name)
            .Select(l => new LabelDto(l.Id, l.ProjectId, l.Name, l.Color))
            .ToListAsync(cancellationToken);
    }

    public async Task<LabelDto> CreateLabelAsync(Guid projectId, CreateLabelRequest request, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        await EnsureProjectMemberAsync(projectId, currentUserId, cancellationToken);

        var normalizedName = request.Name.Trim();

        var exists = await _dbContext.Labels
            .AnyAsync(l => l.ProjectId == projectId && l.Name.ToLower() == normalizedName.ToLower(), cancellationToken);

        if (exists)
        {
            throw new ConflictException($"Label with name '{normalizedName}' already exists in this project.");
        }

        var label = new Label
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            Name = normalizedName,
            Color = string.IsNullOrWhiteSpace(request.Color) ? "#6366F1" : request.Color.Trim()
        };

        _dbContext.Labels.Add(label);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new LabelDto(label.Id, label.ProjectId, label.Name, label.Color);
    }

    public async Task<LabelDto> UpdateLabelAsync(Guid labelId, UpdateLabelRequest request, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var label = await _dbContext.Labels
            .FirstOrDefaultAsync(l => l.Id == labelId, cancellationToken);

        if (label is null)
        {
            throw new NotFoundException(nameof(Label), labelId);
        }

        await EnsureProjectMemberAsync(label.ProjectId, currentUserId, cancellationToken);

        var normalizedName = request.Name.Trim();
        var exists = await _dbContext.Labels
            .AnyAsync(l => l.ProjectId == label.ProjectId && l.Id != labelId && l.Name.ToLower() == normalizedName.ToLower(), cancellationToken);

        if (exists)
        {
            throw new ConflictException($"Label with name '{normalizedName}' already exists in this project.");
        }

        label.Name = normalizedName;
        if (!string.IsNullOrWhiteSpace(request.Color))
        {
            label.Color = request.Color.Trim();
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new LabelDto(label.Id, label.ProjectId, label.Name, label.Color);
    }

    public async Task DeleteLabelAsync(Guid labelId, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var label = await _dbContext.Labels
            .FirstOrDefaultAsync(l => l.Id == labelId, cancellationToken);

        if (label is null)
        {
            throw new NotFoundException(nameof(Label), labelId);
        }

        await EnsureProjectMemberAsync(label.ProjectId, currentUserId, cancellationToken);

        _dbContext.Labels.Remove(label);
        await _dbContext.SaveChangesAsync(cancellationToken);
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
