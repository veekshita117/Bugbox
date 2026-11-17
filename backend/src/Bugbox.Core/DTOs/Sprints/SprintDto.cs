using Bugbox.Core.Enums;

namespace Bugbox.Core.DTOs.Sprints;

public record SprintDto(
    Guid Id,
    Guid ProjectId,
    string Name,
    string? Goal,
    DateTime? StartDate,
    DateTime? EndDate,
    SprintStatus Status,
    int TotalIssuesCount,
    int CompletedIssuesCount
);
