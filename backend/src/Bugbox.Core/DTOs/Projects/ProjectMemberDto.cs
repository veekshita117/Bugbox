using Bugbox.Core.Enums;

namespace Bugbox.Core.DTOs.Projects;

public record ProjectMemberDto(
    Guid UserId,
    string Name,
    string Email,
    ProjectRole Role
);
