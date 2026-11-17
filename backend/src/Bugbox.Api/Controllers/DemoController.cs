using Bugbox.Core.DTOs.Projects;
using Bugbox.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bugbox.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class DemoController : ControllerBase
{
    private readonly IDemoService _demoService;
    private readonly ICurrentUserService _currentUserService;

    public DemoController(IDemoService demoService, ICurrentUserService currentUserService)
    {
        _demoService = demoService;
        _currentUserService = currentUserService;
    }

    [HttpPost("seed")]
    [ProducesResponseType(typeof(ProjectDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ProjectDto>> SeedDemoProject(CancellationToken ct)
    {
        var userId = _currentUserService.UserId!.Value;
        var project = await _demoService.SeedDemoProjectAsync(userId, ct);
        return Created($"/api/projects/{project.Id}", project);
    }
}
