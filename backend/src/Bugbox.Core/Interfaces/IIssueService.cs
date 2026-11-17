using Bugbox.Core.DTOs.Common;
using Bugbox.Core.DTOs.Issues;

namespace Bugbox.Core.Interfaces;

public interface IIssueService
{
    Task<PagedResult<IssueDto>> GetIssuesAsync(Guid projectId, IssueFilterRequest filter, Guid currentUserId, CancellationToken cancellationToken = default);
    Task<IssueDto> GetIssueByIdAsync(Guid issueId, Guid currentUserId, CancellationToken cancellationToken = default);
    Task<IssueDto> CreateIssueAsync(Guid projectId, CreateIssueRequest request, Guid currentUserId, CancellationToken cancellationToken = default);
    Task<IssueDto> UpdateIssueAsync(Guid issueId, UpdateIssueRequest request, Guid currentUserId, CancellationToken cancellationToken = default);
    Task DeleteIssueAsync(Guid issueId, Guid currentUserId, CancellationToken cancellationToken = default);

    Task<IssueDto> UpdateStatusAsync(Guid issueId, UpdateIssueStatusRequest request, Guid currentUserId, CancellationToken cancellationToken = default);
    Task<IssueDto> MoveIssueAsync(Guid issueId, MoveIssueRequest request, Guid currentUserId, CancellationToken cancellationToken = default);

    Task<IssueDto> AddLabelToIssueAsync(Guid issueId, Guid labelId, Guid currentUserId, CancellationToken cancellationToken = default);
    Task<IssueDto> RemoveLabelFromIssueAsync(Guid issueId, Guid labelId, Guid currentUserId, CancellationToken cancellationToken = default);
}
