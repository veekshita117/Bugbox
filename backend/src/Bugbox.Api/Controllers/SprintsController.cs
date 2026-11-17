using Bugbox.Core.DTOs.Sprints;
using Bugbox.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bugbox.Api.Controllers;

[Authorize]
[ApiController]
public class SprintsController : ControllerBase
{
    private readonly ISprintService _sprintService;
    private readonly ICurrentUserService _currentUserService;

    public SprintsController(ISprintService sprintService, ICurrentUserService currentUserService)
    {
        _sprintService = sprintService;
        _currentUserService = currentUserService;
    }

    [HttpGet("api/projects/{projectId:guid}/sprints")]
    [ProducesResponseType(typeof(IReadOnlyList<SprintDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<SprintDto>>> GetProjectSprints(Guid projectId, CancellationToken ct)
    {
        var userId = _currentUserService.UserId!.Value;
        var sprints = await _sprintService.GetProjectSprintsAsync(projectId, userId, ct);
        return Ok(sprints);
    }

    [HttpPost("api/projects/{projectId:guid}/sprints")]
    [ProducesResponseType(typeof(SprintDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SprintDto>> CreateSprint(Guid projectId, [FromBody] CreateSprintRequest request, CancellationToken ct)
    {
        var userId = _currentUserService.UserId!.Value;
        var sprint = await _sprintService.CreateSprintAsync(projectId, request, userId, ct);
        return CreatedAtAction(nameof(GetById), new { id = sprint.Id }, sprint);
    }

    [HttpGet("api/sprints/{id:guid}")]
    [ProducesResponseType(typeof(SprintDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SprintDto>> GetById(Guid id, CancellationToken ct)
    {
        var userId = _currentUserService.UserId!.Value;
        var sprint = await _sprintService.GetSprintByIdAsync(id, userId, ct);
        return Ok(sprint);
    }

    [HttpPut("api/sprints/{id:guid}")]
    [ProducesResponseType(typeof(SprintDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SprintDto>> Update(Guid id, [FromBody] UpdateSprintRequest request, CancellationToken ct)
    {
        var userId = _currentUserService.UserId!.Value;
        var sprint = await _sprintService.UpdateSprintAsync(id, request, userId, ct);
        return Ok(sprint);
    }

    [HttpDelete("api/sprints/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var userId = _currentUserService.UserId!.Value;
        await _sprintService.DeleteSprintAsync(id, userId, ct);
        return NoContent();
    }

    [HttpPost("api/sprints/{id:guid}/start")]
    [ProducesResponseType(typeof(SprintDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SprintDto>> Start(Guid id, CancellationToken ct)
    {
        var userId = _currentUserService.UserId!.Value;
        var sprint = await _sprintService.StartSprintAsync(id, userId, ct);
        return Ok(sprint);
    }

    [HttpPost("api/sprints/{id:guid}/complete")]
    [ProducesResponseType(typeof(SprintDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SprintDto>> Complete(Guid id, [FromBody] CompleteSprintRequest request, CancellationToken ct)
    {
        var userId = _currentUserService.UserId!.Value;
        var sprint = await _sprintService.CompleteSprintAsync(id, request, userId, ct);
        return Ok(sprint);
    }

    [HttpPost("api/sprints/{id:guid}/issues")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddIssues(Guid id, [FromBody] AssignSprintIssuesRequest request, CancellationToken ct)
    {
        var userId = _currentUserService.UserId!.Value;
        await _sprintService.AddIssuesToSprintAsync(id, request, userId, ct);
        return NoContent();
    }

    [HttpDelete("api/sprints/{id:guid}/issues/{issueId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveIssue(Guid id, Guid issueId, CancellationToken ct)
    {
        var userId = _currentUserService.UserId!.Value;
        await _sprintService.RemoveIssueFromSprintAsync(id, issueId, userId, ct);
        return NoContent();
    }

    [HttpGet("api/sprints/{id:guid}/burndown")]
    [ProducesResponseType(typeof(IReadOnlyList<BurndownPointDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<BurndownPointDto>>> GetBurndown(Guid id, CancellationToken ct)
    {
        var userId = _currentUserService.UserId!.Value;
        var burndown = await _sprintService.GetBurndownAsync(id, userId, ct);
        return Ok(burndown);
    }
}
