using Bugbox.Core.DTOs.Common;
using Bugbox.Core.DTOs.Issues;
using Bugbox.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bugbox.Api.Controllers;

[Authorize]
[ApiController]
public class IssuesController : ControllerBase
{
    private readonly IIssueService _issueService;
    private readonly ICurrentUserService _currentUserService;

    public IssuesController(IIssueService issueService, ICurrentUserService currentUserService)
    {
        _issueService = issueService;
        _currentUserService = currentUserService;
    }

    [HttpGet("api/projects/{projectId:guid}/issues")]
    [ProducesResponseType(typeof(PagedResult<IssueDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PagedResult<IssueDto>>> GetProjectIssues(
        Guid projectId,
        [FromQuery] IssueFilterRequest filter,
        CancellationToken ct)
    {
        var userId = _currentUserService.UserId!.Value;
        var result = await _issueService.GetIssuesAsync(projectId, filter, userId, ct);
        return Ok(result);
    }

    [HttpPost("api/projects/{projectId:guid}/issues")]
    [ProducesResponseType(typeof(IssueDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IssueDto>> CreateIssue(
        Guid projectId,
        [FromBody] CreateIssueRequest request,
        CancellationToken ct)
    {
        var userId = _currentUserService.UserId!.Value;
        var issue = await _issueService.CreateIssueAsync(projectId, request, userId, ct);
        return CreatedAtAction(nameof(GetById), new { id = issue.Id }, issue);
    }

    [HttpGet("api/issues/{id:guid}")]
    [ProducesResponseType(typeof(IssueDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IssueDto>> GetById(Guid id, CancellationToken ct)
    {
        var userId = _currentUserService.UserId!.Value;
        var issue = await _issueService.GetIssueByIdAsync(id, userId, ct);
        return Ok(issue);
    }

    [HttpPut("api/issues/{id:guid}")]
    [ProducesResponseType(typeof(IssueDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IssueDto>> Update(
        Guid id,
        [FromBody] UpdateIssueRequest request,
        CancellationToken ct)
    {
        var userId = _currentUserService.UserId!.Value;
        var issue = await _issueService.UpdateIssueAsync(id, request, userId, ct);
        return Ok(issue);
    }

    [HttpDelete("api/issues/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var userId = _currentUserService.UserId!.Value;
        await _issueService.DeleteIssueAsync(id, userId, ct);
        return NoContent();
    }

    [HttpPatch("api/issues/{id:guid}/status")]
    [ProducesResponseType(typeof(IssueDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IssueDto>> UpdateStatus(
        Guid id,
        [FromBody] UpdateIssueStatusRequest request,
        CancellationToken ct)
    {
        var userId = _currentUserService.UserId!.Value;
        var issue = await _issueService.UpdateStatusAsync(id, request, userId, ct);
        return Ok(issue);
    }

    [HttpPatch("api/issues/{id:guid}/move")]
    [ProducesResponseType(typeof(IssueDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IssueDto>> Move(
        Guid id,
        [FromBody] MoveIssueRequest request,
        CancellationToken ct)
    {
        var userId = _currentUserService.UserId!.Value;
        var issue = await _issueService.MoveIssueAsync(id, request, userId, ct);
        return Ok(issue);
    }

    [HttpPost("api/issues/{id:guid}/labels/{labelId:guid}")]
    [ProducesResponseType(typeof(IssueDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IssueDto>> AddLabel(Guid id, Guid labelId, CancellationToken ct)
    {
        var userId = _currentUserService.UserId!.Value;
        var issue = await _issueService.AddLabelToIssueAsync(id, labelId, userId, ct);
        return Ok(issue);
    }

    [HttpDelete("api/issues/{id:guid}/labels/{labelId:guid}")]
    [ProducesResponseType(typeof(IssueDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IssueDto>> RemoveLabel(Guid id, Guid labelId, CancellationToken ct)
    {
        var userId = _currentUserService.UserId!.Value;
        var issue = await _issueService.RemoveLabelFromIssueAsync(id, labelId, userId, ct);
        return Ok(issue);
    }
}
