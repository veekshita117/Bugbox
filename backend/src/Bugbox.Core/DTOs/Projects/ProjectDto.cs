using Bugbox.Core.Enums;

namespace Bugbox.Core.DTOs.Projects;

public record ProjectDto(
    Guid Id,
    string Name,
    string Key,
    string? Description,
    Guid OwnerId,
    DateTime CreatedAt,
    ProjectRole? CurrentUserRole
);
