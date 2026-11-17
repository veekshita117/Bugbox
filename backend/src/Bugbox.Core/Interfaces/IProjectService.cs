using Bugbox.Core.DTOs.Projects;

namespace Bugbox.Core.Interfaces;

public interface IProjectService
{
    Task<IReadOnlyList<ProjectDto>> GetUserProjectsAsync(Guid currentUserId, CancellationToken cancellationToken = default);
    Task<ProjectDto> GetProjectByIdAsync(Guid projectId, Guid currentUserId, CancellationToken cancellationToken = default);
    Task<ProjectDto> CreateProjectAsync(CreateProjectRequest request, Guid currentUserId, CancellationToken cancellationToken = default);
    Task<ProjectDto> UpdateProjectAsync(Guid projectId, UpdateProjectRequest request, Guid currentUserId, CancellationToken cancellationToken = default);
    Task DeleteProjectAsync(Guid projectId, Guid currentUserId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProjectMemberDto>> GetMembersAsync(Guid projectId, Guid currentUserId, CancellationToken cancellationToken = default);
    Task<ProjectMemberDto> AddMemberAsync(Guid projectId, AddProjectMemberRequest request, Guid currentUserId, CancellationToken cancellationToken = default);
    Task RemoveMemberAsync(Guid projectId, Guid memberUserId, Guid currentUserId, CancellationToken cancellationToken = default);
}
