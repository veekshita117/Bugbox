using Bugbox.Core.DTOs.Sprints;

namespace Bugbox.Core.Interfaces;

public interface ISprintService
{
    Task<IReadOnlyList<SprintDto>> GetProjectSprintsAsync(Guid projectId, Guid currentUserId, CancellationToken cancellationToken = default);
    Task<SprintDto> GetSprintByIdAsync(Guid sprintId, Guid currentUserId, CancellationToken cancellationToken = default);
    Task<SprintDto> CreateSprintAsync(Guid projectId, CreateSprintRequest request, Guid currentUserId, CancellationToken cancellationToken = default);
    Task<SprintDto> UpdateSprintAsync(Guid sprintId, UpdateSprintRequest request, Guid currentUserId, CancellationToken cancellationToken = default);
    Task DeleteSprintAsync(Guid sprintId, Guid currentUserId, CancellationToken cancellationToken = default);

    Task<SprintDto> StartSprintAsync(Guid sprintId, Guid currentUserId, CancellationToken cancellationToken = default);
    Task<SprintDto> CompleteSprintAsync(Guid sprintId, CompleteSprintRequest request, Guid currentUserId, CancellationToken cancellationToken = default);

    Task AddIssuesToSprintAsync(Guid sprintId, AssignSprintIssuesRequest request, Guid currentUserId, CancellationToken cancellationToken = default);
    Task RemoveIssueFromSprintAsync(Guid sprintId, Guid issueId, Guid currentUserId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BurndownPointDto>> GetBurndownAsync(Guid sprintId, Guid currentUserId, CancellationToken cancellationToken = default);
}
