using Bugbox.Core.Enums;

namespace Bugbox.Core.DTOs.Issues;

public record IssueFilterRequest(
    IssueStatus? Status = null,
    Guid? AssigneeId = null,
    Guid? LabelId = null,
    Guid? SprintId = null,
    IssueType? Type = null,
    IssuePriority? Priority = null,
    string? Search = null,
    int Page = 1,
    int PageSize = 20
);
