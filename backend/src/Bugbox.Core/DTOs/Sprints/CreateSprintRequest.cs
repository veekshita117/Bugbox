namespace Bugbox.Core.DTOs.Sprints;

public record CreateSprintRequest(
    string Name,
    string? Goal,
    DateTime? StartDate,
    DateTime? EndDate
);
