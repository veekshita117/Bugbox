using Bugbox.Core.DTOs.Auth;

namespace Bugbox.Core.DTOs.Comments;

public record CommentDto(
    Guid Id,
    Guid IssueId,
    UserDto Author,
    string Body,
    DateTime CreatedAt
);
