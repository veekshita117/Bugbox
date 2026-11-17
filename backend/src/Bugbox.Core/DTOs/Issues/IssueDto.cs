using Bugbox.Core.DTOs.Auth;
using Bugbox.Core.DTOs.Labels;
using Bugbox.Core.Enums;

namespace Bugbox.Core.DTOs.Issues;

public record IssueDto(
    Guid Id,
    Guid ProjectId,
    string ProjectKey,
    string IssueKey,
    int Number,
    string Title,
    string? Description,
    IssueType Type,
    IssuePriority Priority,
    IssueStatus Status,
    int BoardOrder,
    UserDto? Assignee,
    UserDto Reporter,
    Guid? SprintId,
    IReadOnlyList<LabelDto> Labels,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    DateTime? CompletedAt,
    int CommentsCount
);
