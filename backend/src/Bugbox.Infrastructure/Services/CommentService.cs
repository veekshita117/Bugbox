using Bugbox.Core.DTOs.Auth;
using Bugbox.Core.DTOs.Comments;
using Bugbox.Core.Entities;
using Bugbox.Core.Enums;
using Bugbox.Core.Exceptions;
using Bugbox.Core.Interfaces;
using Bugbox.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Bugbox.Infrastructure.Services;

public class CommentService : ICommentService
{
    private readonly BugboxDbContext _dbContext;

    public CommentService(BugboxDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<CommentDto>> GetCommentsAsync(Guid issueId, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var issue = await _dbContext.Issues
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == issueId, cancellationToken);

        if (issue is null)
        {
            throw new NotFoundException(nameof(Issue), issueId);
        }

        await EnsureProjectMemberAsync(issue.ProjectId, currentUserId, cancellationToken);

        var comments = await _dbContext.Comments
            .AsNoTracking()
            .Where(c => c.IssueId == issueId)
            .OrderBy(c => c.CreatedAt)
            .Include(c => c.Author)
            .Select(c => new CommentDto(
                c.Id,
                c.IssueId,
                new UserDto(c.Author.Id, c.Author.Name, c.Author.Email, c.Author.CreatedAt),
                c.Body,
                c.CreatedAt
            ))
            .ToListAsync(cancellationToken);

        return comments;
    }

    public async Task<CommentDto> AddCommentAsync(Guid issueId, CreateCommentRequest request, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var issue = await _dbContext.Issues
            .FirstOrDefaultAsync(i => i.Id == issueId, cancellationToken);

        if (issue is null)
        {
            throw new NotFoundException(nameof(Issue), issueId);
        }

        await EnsureProjectMemberAsync(issue.ProjectId, currentUserId, cancellationToken);

        var author = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == currentUserId, cancellationToken);

        if (author is null)
        {
            throw new NotFoundException(nameof(User), currentUserId);
        }

        var comment = new Comment
        {
            Id = Guid.NewGuid(),
            IssueId = issueId,
            AuthorId = currentUserId,
            Author = author,
            Body = request.Body.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Comments.Add(comment);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new CommentDto(
            comment.Id,
            comment.IssueId,
            new UserDto(author.Id, author.Name, author.Email, author.CreatedAt),
            comment.Body,
            comment.CreatedAt
        );
    }

    public async Task DeleteCommentAsync(Guid commentId, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var comment = await _dbContext.Comments
            .Include(c => c.Issue)
            .FirstOrDefaultAsync(c => c.Id == commentId, cancellationToken);

        if (comment is null)
        {
            throw new NotFoundException(nameof(Comment), commentId);
        }

        var member = await _dbContext.ProjectMembers
            .FirstOrDefaultAsync(pm => pm.ProjectId == comment.Issue.ProjectId && pm.UserId == currentUserId, cancellationToken);

        if (member is null)
        {
            throw new ForbiddenException("You do not have access to this project.");
        }

        // Only author or project Admin can delete
        if (comment.AuthorId != currentUserId && member.Role != ProjectRole.Admin)
        {
            throw new ForbiddenException("Only the comment author or a project admin can delete this comment.");
        }

        _dbContext.Comments.Remove(comment);
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
