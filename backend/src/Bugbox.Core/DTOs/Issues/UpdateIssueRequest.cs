using Bugbox.Core.Enums;

namespace Bugbox.Core.DTOs.Issues;

public record UpdateIssueRequest(
    string Title,
    string? Description,
    IssueType Type,
    IssuePriority Priority,
    IssueStatus Status,
    Guid? AssigneeId,
    Guid? SprintId,
    List<Guid>? LabelIds
);
