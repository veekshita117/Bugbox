using Bugbox.Core.DTOs.Issues;
using Bugbox.Core.Entities;
using Bugbox.Core.Enums;
using Bugbox.Core.Exceptions;
using Bugbox.Infrastructure.Data;
using Bugbox.Infrastructure.Services;
using Bugbox.Tests.Helpers;
using FluentAssertions;

namespace Bugbox.Tests.Services;

public class IssueServiceTests
{
    private readonly BugboxDbContext _dbContext;
    private readonly IssueService _sut;
    private readonly User _memberUser;
    private readonly User _outsideUser;
    private readonly Project _projectA;
    private readonly Project _projectB;

    public IssueServiceTests()
    {
        _dbContext = TestDbContextFactory.Create();
        _sut = new IssueService(_dbContext);

        _memberUser = new User { Id = Guid.NewGuid(), Name = "Member", Email = "member@test.com", PasswordHash = "hash" };
        _outsideUser = new User { Id = Guid.NewGuid(), Name = "Outsider", Email = "outsider@test.com", PasswordHash = "hash" };

        _projectA = new Project { Id = Guid.NewGuid(), Name = "Project Alpha", Key = "ALP", OwnerId = _memberUser.Id };
        _projectB = new Project { Id = Guid.NewGuid(), Name = "Project Beta", Key = "BET", OwnerId = _memberUser.Id };

        _dbContext.Users.AddRange(_memberUser, _outsideUser);
        _dbContext.Projects.AddRange(_projectA, _projectB);

        _dbContext.ProjectMembers.Add(new ProjectMember { ProjectId = _projectA.Id, UserId = _memberUser.Id, Role = ProjectRole.Admin });
        _dbContext.ProjectMembers.Add(new ProjectMember { ProjectId = _projectB.Id, UserId = _memberUser.Id, Role = ProjectRole.Admin });

        _dbContext.SaveChanges();
    }

    [Fact]
    public async Task CreateIssueAsync_GeneratesSequentialNumbersPerProject()
    {
        // Act: Create 3 issues in Project Alpha
        var issue1 = await _sut.CreateIssueAsync(_projectA.Id, new CreateIssueRequest("Task 1", null, IssueType.Task, IssuePriority.Medium), _memberUser.Id);
        var issue2 = await _sut.CreateIssueAsync(_projectA.Id, new CreateIssueRequest("Task 2", null, IssueType.Bug, IssuePriority.High), _memberUser.Id);
        var issue3 = await _sut.CreateIssueAsync(_projectA.Id, new CreateIssueRequest("Task 3", null, IssueType.Feature, IssuePriority.Low), _memberUser.Id);

        // Assert: Project Alpha numbering starts at 1 and increments
        issue1.Number.Should().Be(1);
        issue1.IssueKey.Should().Be("ALP-1");

        issue2.Number.Should().Be(2);
        issue2.IssueKey.Should().Be("ALP-2");

        issue3.Number.Should().Be(3);
        issue3.IssueKey.Should().Be("ALP-3");

        // Act: Create issue in Project Beta
        var betaIssue1 = await _sut.CreateIssueAsync(_projectB.Id, new CreateIssueRequest("Beta Task 1", null, IssueType.Task, IssuePriority.Medium), _memberUser.Id);

        // Assert: Independent per-project counter restarts at 1
        betaIssue1.Number.Should().Be(1);
        betaIssue1.IssueKey.Should().Be("BET-1");
    }

    [Fact]
    public async Task CreateIssueAsync_WhenNonMember_ThrowsForbiddenException()
    {
        var act = () => _sut.CreateIssueAsync(
            _projectA.Id,
            new CreateIssueRequest("Hacker Issue", null, IssueType.Bug, IssuePriority.High),
            _outsideUser.Id
        );

        await act.Should().ThrowAsync<ForbiddenException>()
            .WithMessage("You do not have access to this project.");
    }

    [Fact]
    public async Task GetIssueByIdAsync_WhenNonMember_ThrowsForbiddenException()
    {
        var issue = await _sut.CreateIssueAsync(
            _projectA.Id,
            new CreateIssueRequest("Internal Task", null, IssueType.Task, IssuePriority.Low),
            _memberUser.Id
        );

        var act = () => _sut.GetIssueByIdAsync(issue.Id, _outsideUser.Id);

        await act.Should().ThrowAsync<ForbiddenException>()
            .WithMessage("You do not have access to this project.");
    }

    [Fact]
    public async Task UpdateStatusAsync_WhenStatusChangesToDone_SetsCompletedAt()
    {
        // Arrange
        var issue = await _sut.CreateIssueAsync(
            _projectA.Id,
            new CreateIssueRequest("Bug Fix", null, IssueType.Bug, IssuePriority.High, IssueStatus.ToDo),
            _memberUser.Id
        );
        issue.CompletedAt.Should().BeNull();

        // Act: Move to Done
        var updated = await _sut.UpdateStatusAsync(issue.Id, new UpdateIssueStatusRequest(IssueStatus.Done), _memberUser.Id);

        // Assert
        updated.Status.Should().Be(IssueStatus.Done);
        updated.CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateStatusAsync_WhenStatusChangesOutOfDone_ClearsCompletedAt()
    {
        // Arrange: Create in Done
        var issue = await _sut.CreateIssueAsync(
            _projectA.Id,
            new CreateIssueRequest("Done Task", null, IssueType.Task, IssuePriority.Medium, IssueStatus.Done),
            _memberUser.Id
        );
        issue.CompletedAt.Should().NotBeNull();

        // Act: Move back to InProgress
        var updated = await _sut.UpdateStatusAsync(issue.Id, new UpdateIssueStatusRequest(IssueStatus.InProgress), _memberUser.Id);

        // Assert
        updated.Status.Should().Be(IssueStatus.InProgress);
        updated.CompletedAt.Should().BeNull();
    }

    [Fact]
    public async Task MoveIssueAsync_UpdatesStatusAndBoardOrderTogether()
    {
        // Arrange
        var issue = await _sut.CreateIssueAsync(
            _projectA.Id,
            new CreateIssueRequest("Card", null, IssueType.Task, IssuePriority.Medium, IssueStatus.ToDo),
            _memberUser.Id
        );

        // Act
        var moved = await _sut.MoveIssueAsync(issue.Id, new MoveIssueRequest(IssueStatus.InProgress, 3), _memberUser.Id);

        // Assert
        moved.Status.Should().Be(IssueStatus.InProgress);
        moved.BoardOrder.Should().Be(3);
        moved.CompletedAt.Should().BeNull();
    }
}
