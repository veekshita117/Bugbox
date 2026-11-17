using Bugbox.Core.DTOs.Projects;
using Bugbox.Core.Entities;
using Bugbox.Core.Enums;
using Bugbox.Core.Exceptions;
using Bugbox.Infrastructure.Data;
using Bugbox.Infrastructure.Services;
using Bugbox.Tests.Helpers;
using FluentAssertions;

namespace Bugbox.Tests.Services;

public class ProjectServiceTests
{
    private readonly BugboxDbContext _dbContext;
    private readonly ProjectService _sut;
    private readonly User _ownerUser;
    private readonly User _memberUser;
    private readonly User _outsideUser;

    public ProjectServiceTests()
    {
        _dbContext = TestDbContextFactory.Create();
        _sut = new ProjectService(_dbContext);

        _ownerUser = new User { Id = Guid.NewGuid(), Name = "Owner", Email = "owner@test.com", PasswordHash = "hash" };
        _memberUser = new User { Id = Guid.NewGuid(), Name = "Member", Email = "member@test.com", PasswordHash = "hash" };
        _outsideUser = new User { Id = Guid.NewGuid(), Name = "Outsider", Email = "outsider@test.com", PasswordHash = "hash" };

        _dbContext.Users.AddRange(_ownerUser, _memberUser, _outsideUser);
        _dbContext.SaveChanges();
    }

    [Fact]
    public async Task CreateProjectAsync_SetsCreatorAsAdmin()
    {
        // Act
        var project = await _sut.CreateProjectAsync(
            new CreateProjectRequest("Bugbox Core", "BUG", "Project description"),
            _ownerUser.Id
        );

        // Assert
        project.Should().NotBeNull();
        project.Key.Should().Be("BUG");
        project.CurrentUserRole.Should().Be(ProjectRole.Admin);

        var membership = _dbContext.ProjectMembers.SingleOrDefault(pm => pm.ProjectId == project.Id && pm.UserId == _ownerUser.Id);
        membership.Should().NotBeNull();
        membership!.Role.Should().Be(ProjectRole.Admin);
    }

    [Fact]
    public async Task GetUserProjectsAsync_ReturnsOnlyProjectsUserBelongsTo()
    {
        // Arrange
        var p1 = await _sut.CreateProjectAsync(new CreateProjectRequest("Project 1", "P1", null), _ownerUser.Id);
        var p2 = await _sut.CreateProjectAsync(new CreateProjectRequest("Project 2", "P2", null), _outsideUser.Id);

        // Add memberUser to p1 only
        await _sut.AddMemberAsync(p1.Id, new AddProjectMemberRequest(_memberUser.Email, ProjectRole.Member), _ownerUser.Id);

        // Act
        var memberProjects = await _sut.GetUserProjectsAsync(_memberUser.Id);

        // Assert
        memberProjects.Should().HaveCount(1);
        memberProjects.Single().Key.Should().Be("P1");
        memberProjects.Single().CurrentUserRole.Should().Be(ProjectRole.Member);
    }

    [Fact]
    public async Task GetProjectByIdAsync_WhenNonMember_ThrowsForbiddenException()
    {
        // Arrange
        var project = await _sut.CreateProjectAsync(new CreateProjectRequest("Secret", "SEC", null), _ownerUser.Id);

        // Act
        var act = () => _sut.GetProjectByIdAsync(project.Id, _outsideUser.Id);

        // Assert
        await act.Should().ThrowAsync<ForbiddenException>()
            .WithMessage("You do not have access to this project.");
    }

    [Fact]
    public async Task UpdateProjectAsync_WhenNonAdmin_ThrowsForbiddenException()
    {
        // Arrange
        var project = await _sut.CreateProjectAsync(new CreateProjectRequest("Open", "OPN", null), _ownerUser.Id);
        await _sut.AddMemberAsync(project.Id, new AddProjectMemberRequest(_memberUser.Email, ProjectRole.Member), _ownerUser.Id);

        // Act
        var act = () => _sut.UpdateProjectAsync(project.Id, new UpdateProjectRequest("Renamed", null), _memberUser.Id);

        // Assert
        await act.Should().ThrowAsync<ForbiddenException>()
            .WithMessage("Only project admins can modify project details.");
    }

    [Fact]
    public async Task DeleteProjectAsync_WhenNonAdmin_ThrowsForbiddenException()
    {
        // Arrange
        var project = await _sut.CreateProjectAsync(new CreateProjectRequest("Open", "OPN", null), _ownerUser.Id);
        await _sut.AddMemberAsync(project.Id, new AddProjectMemberRequest(_memberUser.Email, ProjectRole.Member), _ownerUser.Id);

        // Act
        var act = () => _sut.DeleteProjectAsync(project.Id, _memberUser.Id);

        // Assert
        await act.Should().ThrowAsync<ForbiddenException>()
            .WithMessage("Only project admins can delete a project.");
    }

    [Fact]
    public async Task AddMemberAsync_WhenNonAdmin_ThrowsForbiddenException()
    {
        // Arrange
        var project = await _sut.CreateProjectAsync(new CreateProjectRequest("Team", "TM", null), _ownerUser.Id);
        await _sut.AddMemberAsync(project.Id, new AddProjectMemberRequest(_memberUser.Email, ProjectRole.Member), _ownerUser.Id);

        // Act: member attempts to add outsider
        var act = () => _sut.AddMemberAsync(project.Id, new AddProjectMemberRequest(_outsideUser.Email, ProjectRole.Member), _memberUser.Id);

        // Assert
        await act.Should().ThrowAsync<ForbiddenException>()
            .WithMessage("Only project admins can add members.");
    }

    [Fact]
    public async Task RemoveMemberAsync_WhenNonAdmin_ThrowsForbiddenException()
    {
        // Arrange
        var project = await _sut.CreateProjectAsync(new CreateProjectRequest("Team", "TM", null), _ownerUser.Id);
        await _sut.AddMemberAsync(project.Id, new AddProjectMemberRequest(_memberUser.Email, ProjectRole.Member), _ownerUser.Id);

        // Act: member attempts to remove owner
        var act = () => _sut.RemoveMemberAsync(project.Id, _ownerUser.Id, _memberUser.Id);

        // Assert
        await act.Should().ThrowAsync<ForbiddenException>()
            .WithMessage("Only project admins can remove members.");
    }
}
