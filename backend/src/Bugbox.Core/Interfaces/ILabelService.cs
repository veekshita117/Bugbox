using Bugbox.Core.DTOs.Labels;

namespace Bugbox.Core.Interfaces;

public interface ILabelService
{
    Task<IReadOnlyList<LabelDto>> GetLabelsAsync(Guid projectId, Guid currentUserId, CancellationToken cancellationToken = default);
    Task<LabelDto> CreateLabelAsync(Guid projectId, CreateLabelRequest request, Guid currentUserId, CancellationToken cancellationToken = default);
    Task<LabelDto> UpdateLabelAsync(Guid labelId, UpdateLabelRequest request, Guid currentUserId, CancellationToken cancellationToken = default);
    Task DeleteLabelAsync(Guid labelId, Guid currentUserId, CancellationToken cancellationToken = default);
}
