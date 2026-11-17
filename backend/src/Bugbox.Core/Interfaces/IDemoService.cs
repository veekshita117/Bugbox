using Bugbox.Core.DTOs.Projects;

namespace Bugbox.Core.Interfaces;

public interface IDemoService
{
    Task<ProjectDto> SeedDemoProjectAsync(Guid currentUserId, CancellationToken cancellationToken = default);
}
