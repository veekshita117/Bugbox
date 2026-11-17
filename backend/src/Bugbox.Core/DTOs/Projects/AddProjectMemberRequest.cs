using Bugbox.Core.Enums;

namespace Bugbox.Core.DTOs.Projects;

public record AddProjectMemberRequest(string Email, ProjectRole Role = ProjectRole.Member);
