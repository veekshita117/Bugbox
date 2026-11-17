using Bugbox.Core.DTOs.Sprints;
using Bugbox.Core.Entities;
using Bugbox.Core.Enums;
using Bugbox.Core.Exceptions;
using Bugbox.Infrastructure.Data;
using Bugbox.Infrastructure.Services;
using Bugbox.Tests.Helpers;
using FluentAssertions;

namespace Bugbox.Tests.Services;

public class SprintServiceTests
{
    private readonly BugboxDbContext _dbContext;
    private readonly SprintService _sut;
    private readonly User _user;
    private readonly Project _project;

    public SprintServiceTests()
    {
        _dbContext = TestDbContextFactory.Create();
        _sut = new SprintService(_dbContext);

        _user = new User { Id = Guid.NewGuid(), Name = "Developer", Email = "dev@test.com", PasswordHash = "hash" };
        _project = new Project { Id = Guid.NewGuid(), Name = "Project", Key = "PRJ", OwnerId = _user.Id };

        _dbContext.Users.Add(_user);
        _dbContext.Projects.Add(_project);
        _dbContext.ProjectMembers.Add(new ProjectMember { ProjectId = _project.Id, UserId = _user.Id, Role = ProjectRole.Admin });

        _dbContext.SaveChanges();
    }

    [Fact]
    public async Task DeleteSprintAsync_WhenSprintIsPlanned_Succeeds()
    {
        // Arrange
        var sprint = await _sut.CreateSprintAsync(
            _project.Id,
            new CreateSprintRequest("Sprint 1", null, null, null),
            _user.Id
        );

        // Act
        await _sut.DeleteSprintAsync(sprint.Id, _user.Id);

        // Assert
        var sprints = await _sut.GetProjectSprintsAsync(_project.Id, _user.Id);
        sprints.Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteSprintAsync_WhenSprintIsActiveOrCompleted_ThrowsBadRequestException()
    {
        // Arrange
        var sprint = await _sut.CreateSprintAsync(
            _project.Id,
            new CreateSprintRequest("Sprint 1", null, DateTime.UtcNow, DateTime.UtcNow.AddDays(14)),
            _user.Id
        );
        await _sut.StartSprintAsync(sprint.Id, _user.Id);

        // Act: try to delete Active sprint
        var act = () => _sut.DeleteSprintAsync(sprint.Id, _user.Id);

        // Assert
        await act.Should().ThrowAsync<BadRequestException>()
            .WithMessage("Only planned sprints can be deleted.");
    }

    [Fact]
    public async Task StartSprintAsync_WhenAnotherSprintIsAlreadyActive_ThrowsConflictException()
    {
        // Arrange
        var sprint1 = await _sut.CreateSprintAsync(_project.Id, new CreateSprintRequest("Sprint 1", null, null, null), _user.Id);
        var sprint2 = await _sut.CreateSprintAsync(_project.Id, new CreateSprintRequest("Sprint 2", null, null, null), _user.Id);

        await _sut.StartSprintAsync(sprint1.Id, _user.Id);

        // Act: start sprint2 while sprint1 is active
        var act = () => _sut.StartSprintAsync(sprint2.Id, _user.Id);

        // Assert
        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("*Another sprint is currently active*");
    }

    [Fact]
    public async Task CompleteSprintAsync_MovesUnfinishedIssuesToBacklogOrTargetSprint()
    {
        // Arrange
        var sprint1 = await _sut.CreateSprintAsync(_project.Id, new CreateSprintRequest("Sprint 1", null, null, null), _user.Id);
        var sprint2 = await _sut.CreateSprintAsync(_project.Id, new CreateSprintRequest("Sprint 2", null, null, null), _user.Id);

        var issueDone = new Issue { Id = Guid.NewGuid(), ProjectId = _project.Id, SprintId = sprint1.Id, Title = "Done", Status = IssueStatus.Done, Number = 1, ReporterId = _user.Id };
        var issueUnfinished1 = new Issue { Id = Guid.NewGuid(), ProjectId = _project.Id, SprintId = sprint1.Id, Title = "InProg", Status = IssueStatus.InProgress, Number = 2, ReporterId = _user.Id };
        var issueUnfinished2 = new Issue { Id = Guid.NewGuid(), ProjectId = _project.Id, SprintId = sprint1.Id, Title = "ToDo", Status = IssueStatus.ToDo, Number = 3, ReporterId = _user.Id };

        _dbContext.Issues.AddRange(issueDone, issueUnfinished1, issueUnfinished2);
        await _dbContext.SaveChangesAsync();

        await _sut.StartSprintAsync(sprint1.Id, _user.Id);

        // Act: complete sprint1 and move unfinished to sprint2
        await _sut.CompleteSprintAsync(sprint1.Id, new CompleteSprintRequest(sprint2.Id), _user.Id);

        // Assert
        var refreshedDone = await _dbContext.Issues.FindAsync(issueDone.Id);
        var refreshedUnfinished1 = await _dbContext.Issues.FindAsync(issueUnfinished1.Id);

        refreshedDone!.SprintId.Should().Be(sprint1.Id); // Done stays in completed sprint
        refreshedUnfinished1!.SprintId.Should().Be(sprint2.Id); // Unfinished moved to target
    }

    [Fact]
    public async Task CompleteSprintAsync_WithoutTargetSprint_MovesUnfinishedIssuesToBacklog()
    {
        // Arrange
        var sprint = await _sut.CreateSprintAsync(_project.Id, new CreateSprintRequest("Sprint 1", null, null, null), _user.Id);
        var issue = new Issue { Id = Guid.NewGuid(), ProjectId = _project.Id, SprintId = sprint.Id, Title = "Pending", Status = IssueStatus.ToDo, Number = 1, ReporterId = _user.Id };

        _dbContext.Issues.Add(issue);
        await _dbContext.SaveChangesAsync();

        await _sut.StartSprintAsync(sprint.Id, _user.Id);

        // Act: complete without target sprint
        await _sut.CompleteSprintAsync(sprint.Id, new CompleteSprintRequest(null), _user.Id);

        // Assert
        var refreshed = await _dbContext.Issues.FindAsync(issue.Id);
        refreshed!.SprintId.Should().BeNull(); // Returned to backlog
    }

    [Fact]
    public async Task GetBurndownAsync_WithNoIssues_ReturnsAllZeroes()
    {
        // Arrange: 5-day sprint with 0 issues
        var startDate = new DateTime(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc);
        var endDate = new DateTime(2026, 11, 5, 0, 0, 0, DateTimeKind.Utc);

        var sprint = await _sut.CreateSprintAsync(_project.Id, new CreateSprintRequest("Empty Sprint", null, startDate, endDate), _user.Id);

        // Act
        var burndown = await _sut.GetBurndownAsync(sprint.Id, _user.Id);

        // Assert
        burndown.Should().HaveCount(5); // Nov 1 through Nov 5 inclusive (5 days)
        burndown.Should().OnlyContain(p => p.Remaining == 0 && p.Ideal == 0.0);
    }

    [Fact]
    public async Task GetBurndownAsync_WithIssuesCompletedOnDayOne_ReflectsImmediateDrop()
    {
        // Arrange: 4-day sprint (Nov 1 to Nov 4) with 2 issues, 1 completed on Day 1
        var startDate = new DateTime(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc);
        var endDate = new DateTime(2026, 11, 4, 0, 0, 0, DateTimeKind.Utc);

        var sprint = new Sprint
        {
            Id = Guid.NewGuid(),
            ProjectId = _project.Id,
            Name = "Sprint Fast",
            StartDate = startDate,
            EndDate = endDate,
            Status = SprintStatus.Active
        };

        var issue1 = new Issue
        {
            Id = Guid.NewGuid(),
            ProjectId = _project.Id,
            SprintId = sprint.Id,
            Title = "Completed Day 1",
            Status = IssueStatus.Done,
            CompletedAt = startDate, // Completed on day one
            Number = 1,
            ReporterId = _user.Id
        };

        var issue2 = new Issue
        {
            Id = Guid.NewGuid(),
            ProjectId = _project.Id,
            SprintId = sprint.Id,
            Title = "Remaining Issue",
            Status = IssueStatus.InProgress,
            CompletedAt = null,
            Number = 2,
            ReporterId = _user.Id
        };

        sprint.Issues.Add(issue1);
        sprint.Issues.Add(issue2);

        _dbContext.Sprints.Add(sprint);
        await _dbContext.SaveChangesAsync();

        // Act
        var burndown = await _sut.GetBurndownAsync(sprint.Id, _user.Id);

        // Assert
        burndown.Should().HaveCount(4);
        // Day 1 (Nov 1): 2 total - 1 completed = 1 remaining. Ideal = 2.0
        burndown[0].Date.Should().Be("2026-11-01");
        burndown[0].Remaining.Should().Be(1);
        burndown[0].Ideal.Should().Be(2.0);

        // Day 4 (Nov 4): Remaining is still 1. Ideal = 0.0
        burndown[3].Date.Should().Be("2026-11-04");
        burndown[3].Remaining.Should().Be(1);
        burndown[3].Ideal.Should().Be(0.0);
    }

    [Fact]
    public async Task GetBurndownAsync_WithReopenedIssue_CountsAsRemaining()
    {
        // Arrange: 3-day sprint. An issue had CompletedAt in the past, but is now InProgress (re-opened)
        var startDate = new DateTime(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc);
        var endDate = new DateTime(2026, 11, 3, 0, 0, 0, DateTimeKind.Utc);

        var sprint = new Sprint
        {
            Id = Guid.NewGuid(),
            ProjectId = _project.Id,
            Name = "Sprint Reopen",
            StartDate = startDate,
            EndDate = endDate,
            Status = SprintStatus.Active
        };

        var reopenedIssue = new Issue
        {
            Id = Guid.NewGuid(),
            ProjectId = _project.Id,
            SprintId = sprint.Id,
            Title = "Reopened Bug",
            Status = IssueStatus.InProgress, // Re-opened!
            CompletedAt = null,             // Cleared when moved out of Done
            Number = 1,
            ReporterId = _user.Id
        };

        sprint.Issues.Add(reopenedIssue);
        _dbContext.Sprints.Add(sprint);
        await _dbContext.SaveChangesAsync();

        // Act
        var burndown = await _sut.GetBurndownAsync(sprint.Id, _user.Id);

        // Assert: because it is re-opened (Status != Done), remaining stays at 1 on all days
        burndown.Should().HaveCount(3);
        burndown.Should().OnlyContain(p => p.Remaining == 1);
    }
}
