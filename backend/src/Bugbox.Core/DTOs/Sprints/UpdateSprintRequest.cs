namespace Bugbox.Core.DTOs.Sprints;

public record UpdateSprintRequest(
    string Name,
    string? Goal,
    DateTime? StartDate,
    DateTime? EndDate
);
