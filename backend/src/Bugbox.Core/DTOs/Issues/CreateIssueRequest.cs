using Bugbox.Core.Enums;

namespace Bugbox.Core.DTOs.Issues;

public record CreateIssueRequest(
    string Title,
    string? Description,
    IssueType Type,
    IssuePriority Priority,
    IssueStatus Status = IssueStatus.ToDo,
    Guid? AssigneeId = null,
    Guid? SprintId = null,
    List<Guid>? LabelIds = null
);
