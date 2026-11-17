using Bugbox.Core.DTOs.Comments;
using Bugbox.Core.Entities;
using Bugbox.Core.Enums;
using Bugbox.Core.Exceptions;
using Bugbox.Infrastructure.Data;
using Bugbox.Infrastructure.Services;
using Bugbox.Tests.Helpers;
using FluentAssertions;

namespace Bugbox.Tests.Services;

public class CommentServiceTests
{
    private readonly BugboxDbContext _dbContext;
    private readonly CommentService _sut;
    private readonly User _adminUser;
    private readonly User _authorUser;
    private readonly User _memberUser;
    private readonly User _outsideUser;
    private readonly Project _project;
    private readonly Issue _issue;

    public CommentServiceTests()
    {
        _dbContext = TestDbContextFactory.Create();
        _sut = new CommentService(_dbContext);

        _adminUser = new User { Id = Guid.NewGuid(), Name = "Admin", Email = "admin@test.com", PasswordHash = "hash" };
        _authorUser = new User { Id = Guid.NewGuid(), Name = "Author", Email = "author@test.com", PasswordHash = "hash" };
        _memberUser = new User { Id = Guid.NewGuid(), Name = "OtherMember", Email = "other@test.com", PasswordHash = "hash" };
        _outsideUser = new User { Id = Guid.NewGuid(), Name = "Outsider", Email = "outsider@test.com", PasswordHash = "hash" };

        _project = new Project { Id = Guid.NewGuid(), Name = "Project", Key = "PRJ", OwnerId = _adminUser.Id };
        _issue = new Issue { Id = Guid.NewGuid(), ProjectId = _project.Id, Title = "Issue", Number = 1, ReporterId = _adminUser.Id };

        _dbContext.Users.AddRange(_adminUser, _authorUser, _memberUser, _outsideUser);
        _dbContext.Projects.Add(_project);
        _dbContext.Issues.Add(_issue);

        _dbContext.ProjectMembers.Add(new ProjectMember { ProjectId = _project.Id, UserId = _adminUser.Id, Role = ProjectRole.Admin });
        _dbContext.ProjectMembers.Add(new ProjectMember { ProjectId = _project.Id, UserId = _authorUser.Id, Role = ProjectRole.Member });
        _dbContext.ProjectMembers.Add(new ProjectMember { ProjectId = _project.Id, UserId = _memberUser.Id, Role = ProjectRole.Member });

        _dbContext.SaveChanges();
    }

    [Fact]
    public async Task AddCommentAsync_WhenAuthorIsMember_AddsCommentSuccessfully()
    {
        var comment = await _sut.AddCommentAsync(_issue.Id, new CreateCommentRequest("Great progress!"), _authorUser.Id);

        comment.Should().NotBeNull();
        comment.Body.Should().Be("Great progress!");
        comment.Author.Id.Should().Be(_authorUser.Id);
    }

    [Fact]
    public async Task AddCommentAsync_WhenAuthorIsNonMember_ThrowsForbiddenException()
    {
        var act = () => _sut.AddCommentAsync(_issue.Id, new CreateCommentRequest("Spam comment"), _outsideUser.Id);

        await act.Should().ThrowAsync<ForbiddenException>()
            .WithMessage("You do not have access to this project.");
    }

    [Fact]
    public async Task DeleteCommentAsync_WhenAuthorDeletesOwnComment_Succeeds()
    {
        var comment = await _sut.AddCommentAsync(_issue.Id, new CreateCommentRequest("My note"), _authorUser.Id);

        await _sut.DeleteCommentAsync(comment.Id, _authorUser.Id);

        var comments = await _sut.GetCommentsAsync(_issue.Id, _authorUser.Id);
        comments.Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteCommentAsync_WhenAdminDeletesOtherMemberComment_Succeeds()
    {
        var comment = await _sut.AddCommentAsync(_issue.Id, new CreateCommentRequest("Author note"), _authorUser.Id);

        // Admin deletes author's comment
        await _sut.DeleteCommentAsync(comment.Id, _adminUser.Id);

        var comments = await _sut.GetCommentsAsync(_issue.Id, _adminUser.Id);
        comments.Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteCommentAsync_WhenNonAuthorMemberDeletes_ThrowsForbiddenException()
    {
        var comment = await _sut.AddCommentAsync(_issue.Id, new CreateCommentRequest("Author note"), _authorUser.Id);

        // Another regular member tries to delete author's comment
        var act = () => _sut.DeleteCommentAsync(comment.Id, _memberUser.Id);

        await act.Should().ThrowAsync<ForbiddenException>()
            .WithMessage("Only the comment author or a project admin can delete this comment.");
    }
}
