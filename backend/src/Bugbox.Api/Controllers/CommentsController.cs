using Bugbox.Core.DTOs.Comments;
using Bugbox.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bugbox.Api.Controllers;

[Authorize]
[ApiController]
public class CommentsController : ControllerBase
{
    private readonly ICommentService _commentService;
    private readonly ICurrentUserService _currentUserService;

    public CommentsController(ICommentService commentService, ICurrentUserService currentUserService)
    {
        _commentService = commentService;
        _currentUserService = currentUserService;
    }

    [HttpGet("api/issues/{issueId:guid}/comments")]
    [ProducesResponseType(typeof(IReadOnlyList<CommentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<CommentDto>>> GetComments(Guid issueId, CancellationToken ct)
    {
        var userId = _currentUserService.UserId!.Value;
        var comments = await _commentService.GetCommentsAsync(issueId, userId, ct);
        return Ok(comments);
    }

    [HttpPost("api/issues/{issueId:guid}/comments")]
    [ProducesResponseType(typeof(CommentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CommentDto>> AddComment(Guid issueId, [FromBody] CreateCommentRequest request, CancellationToken ct)
    {
        var userId = _currentUserService.UserId!.Value;
        var comment = await _commentService.AddCommentAsync(issueId, request, userId, ct);
        return Created($"/api/comments/{comment.Id}", comment);
    }

    [HttpDelete("api/comments/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteComment(Guid id, CancellationToken ct)
    {
        var userId = _currentUserService.UserId!.Value;
        await _commentService.DeleteCommentAsync(id, userId, ct);
        return NoContent();
    }
}
