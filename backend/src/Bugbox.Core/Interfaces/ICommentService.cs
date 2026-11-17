using Bugbox.Core.DTOs.Comments;

namespace Bugbox.Core.Interfaces;

public interface ICommentService
{
    Task<IReadOnlyList<CommentDto>> GetCommentsAsync(Guid issueId, Guid currentUserId, CancellationToken cancellationToken = default);
    Task<CommentDto> AddCommentAsync(Guid issueId, CreateCommentRequest request, Guid currentUserId, CancellationToken cancellationToken = default);
    Task DeleteCommentAsync(Guid commentId, Guid currentUserId, CancellationToken cancellationToken = default);
}
