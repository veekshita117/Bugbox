using Bugbox.Core.DTOs.Labels;
using Bugbox.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bugbox.Api.Controllers;

[Authorize]
[ApiController]
public class LabelsController : ControllerBase
{
    private readonly ILabelService _labelService;
    private readonly ICurrentUserService _currentUserService;

    public LabelsController(ILabelService labelService, ICurrentUserService currentUserService)
    {
        _labelService = labelService;
        _currentUserService = currentUserService;
    }

    [HttpGet("api/projects/{projectId:guid}/labels")]
    [ProducesResponseType(typeof(IReadOnlyList<LabelDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<LabelDto>>> GetProjectLabels(Guid projectId, CancellationToken ct)
    {
        var userId = _currentUserService.UserId!.Value;
        var labels = await _labelService.GetLabelsAsync(projectId, userId, ct);
        return Ok(labels);
    }

    [HttpPost("api/projects/{projectId:guid}/labels")]
    [ProducesResponseType(typeof(LabelDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<LabelDto>> CreateLabel(Guid projectId, [FromBody] CreateLabelRequest request, CancellationToken ct)
    {
        var userId = _currentUserService.UserId!.Value;
        var label = await _labelService.CreateLabelAsync(projectId, request, userId, ct);
        return Created($"/api/labels/{label.Id}", label);
    }

    [HttpPut("api/labels/{id:guid}")]
    [ProducesResponseType(typeof(LabelDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<LabelDto>> UpdateLabel(Guid id, [FromBody] UpdateLabelRequest request, CancellationToken ct)
    {
        var userId = _currentUserService.UserId!.Value;
        var label = await _labelService.UpdateLabelAsync(id, request, userId, ct);
        return Ok(label);
    }

    [HttpDelete("api/labels/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteLabel(Guid id, CancellationToken ct)
    {
        var userId = _currentUserService.UserId!.Value;
        await _labelService.DeleteLabelAsync(id, userId, ct);
        return NoContent();
    }
}
