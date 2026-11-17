using Bugbox.Core.DTOs.Projects;
using Bugbox.Core.Entities;
using Bugbox.Core.Enums;
using Bugbox.Core.Interfaces;
using Bugbox.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Bugbox.Infrastructure.Services;

public class DemoService : IDemoService
{
    private readonly BugboxDbContext _dbContext;

    public DemoService(BugboxDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ProjectDto> SeedDemoProjectAsync(Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var currentUser = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == currentUserId, cancellationToken);

        if (currentUser is null)
        {
            throw new KeyNotFoundException("Current user not found.");
        }

        // Generate unique key if MOB already exists for someone else
        var baseKey = "MOB";
        var candidateKey = baseKey;
        var counter = 1;
        while (await _dbContext.Projects.AnyAsync(p => p.Key == candidateKey, cancellationToken))
        {
            candidateKey = $"{baseKey}{counter++}";
        }

        var now = DateTime.UtcNow;

        // 1. Create Project
        var project = new Project
        {
            Id = Guid.NewGuid(),
            Name = "Bugbox Mobile Client",
            Key = candidateKey,
            Description = "Cross-platform mobile issue tracker built with React Native and ASP.NET Core Web API.",
            OwnerId = currentUserId,
            CreatedAt = now.AddDays(-28)
        };

        // 2. Add Owner Member
        project.Members.Add(new ProjectMember
        {
            ProjectId = project.Id,
            UserId = currentUserId,
            Role = ProjectRole.Admin
        });

        // 3. Create or Link Demo Team Members
        var sarah = await GetOrCreateUserAsync("Sarah Chen", "sarah.chen@demo.bugbox.dev", cancellationToken);
        var marcus = await GetOrCreateUserAsync("Marcus Rivera", "marcus.r@demo.bugbox.dev", cancellationToken);
        var elena = await GetOrCreateUserAsync("Elena Rostova", "elena.qa@demo.bugbox.dev", cancellationToken);

        project.Members.Add(new ProjectMember { ProjectId = project.Id, UserId = sarah.Id, Role = ProjectRole.Member });
        project.Members.Add(new ProjectMember { ProjectId = project.Id, UserId = marcus.Id, Role = ProjectRole.Member });
        project.Members.Add(new ProjectMember { ProjectId = project.Id, UserId = elena.Id, Role = ProjectRole.Member });

        // 4. Create Labels
        var labelFrontend = new Label { Id = Guid.NewGuid(), ProjectId = project.Id, Name = "frontend", Color = "#6366F1" };
        var labelBackend = new Label { Id = Guid.NewGuid(), ProjectId = project.Id, Name = "backend", Color = "#10B981" };
        var labelDesign = new Label { Id = Guid.NewGuid(), ProjectId = project.Id, Name = "design", Color = "#EC4899" };
        var labelCritical = new Label { Id = Guid.NewGuid(), ProjectId = project.Id, Name = "critical", Color = "#EF4444" };
        var labelPerformance = new Label { Id = Guid.NewGuid(), ProjectId = project.Id, Name = "performance", Color = "#F59E0B" };

        project.Labels.Add(labelFrontend);
        project.Labels.Add(labelBackend);
        project.Labels.Add(labelDesign);
        project.Labels.Add(labelCritical);
        project.Labels.Add(labelPerformance);

        // 5. Create Completed Sprint
        var sprint1 = new Sprint
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            Name = "Sprint 1 - Foundation & Offline Sync",
            Goal = "Establish biometric authentication, secure JWT storage, and SQLite offline cache.",
            StartDate = now.AddDays(-21),
            EndDate = now.AddDays(-7),
            Status = SprintStatus.Completed
        };
        project.Sprints.Add(sprint1);

        // 6. Create Active Sprint
        var sprint2 = new Sprint
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            Name = "Sprint 2 - Kanban Board & Gestures",
            Goal = "Implement drag-and-drop columns, optimistic status updates, and interactive issue modals.",
            StartDate = now.AddDays(-5),
            EndDate = now.AddDays(9),
            Status = SprintStatus.Active
        };
        project.Sprints.Add(sprint2);

        // 7. Add Issues to Sprint 1 (All Done)
        var s1Issue1 = CreateIssue(project.Id, sprint1.Id, 1, "Setup React Native architecture with TypeScript", "Configure navigation stacks, Zustand store, and ESLint configs.", IssueType.Task, IssuePriority.High, IssueStatus.Done, sarah.Id, currentUserId, 0, now.AddDays(-20), now.AddDays(-18), labelFrontend);
        var s1Issue2 = CreateIssue(project.Id, sprint1.Id, 2, "Implement biometric keychain storage for JWT", "Use SecureStore on iOS and Keystore on Android.", IssueType.Feature, IssuePriority.High, IssueStatus.Done, sarah.Id, currentUserId, 1, now.AddDays(-20), now.AddDays(-14), labelBackend);
        var s1Issue3 = CreateIssue(project.Id, sprint1.Id, 3, "Design tokens and typography system", "Setup neutral slate palette and indigo brand accents in Tailwind.", IssueType.Task, IssuePriority.Medium, IssueStatus.Done, marcus.Id, currentUserId, 2, now.AddDays(-19), now.AddDays(-11), labelDesign);
        var s1Issue4 = CreateIssue(project.Id, sprint1.Id, 4, "Automated smoke tests for token refresh", "Verify silent 401 interceptor refresh pipeline with Vitest.", IssueType.Task, IssuePriority.Low, IssueStatus.Done, elena.Id, currentUserId, 3, now.AddDays(-18), now.AddDays(-8), labelFrontend);

        project.Issues.Add(s1Issue1);
        project.Issues.Add(s1Issue2);
        project.Issues.Add(s1Issue3);
        project.Issues.Add(s1Issue4);

        // 8. Add Issues to Sprint 2 (Active Sprint)
        // Done issues (with CompletedAt dates)
        var s2Issue1 = CreateIssue(project.Id, sprint2.Id, 5, "Kanban column swipe gestures", "Enable smooth touch swipe between ToDo, InProgress, and InReview columns.", IssueType.Feature, IssuePriority.High, IssueStatus.Done, sarah.Id, currentUserId, 0, now.AddDays(-5), now.AddDays(-3), labelFrontend);
        var s2Issue2 = CreateIssue(project.Id, sprint2.Id, 6, "Fix memory leak in Recharts canvas container", "Clean up resize listeners when switching project tabs.", IssueType.Bug, IssuePriority.Medium, IssueStatus.Done, sarah.Id, currentUserId, 1, now.AddDays(-4), now.AddDays(-1), labelPerformance);

        // InReview issue
        var s2Issue3 = CreateIssue(project.Id, sprint2.Id, 7, "Pull-to-refresh cache invalidation", "Trigger TanStack Query refetch when pulling top of column.", IssueType.Feature, IssuePriority.Medium, IssueStatus.InReview, marcus.Id, currentUserId, 0, now.AddDays(-3), null, labelFrontend);

        // InProgress issues
        var s2Issue4 = CreateIssue(project.Id, sprint2.Id, 8, "Optimistic board movement rollback on 500 error", "Ensure failed network requests revert card back to original column with error toast.", IssueType.Bug, IssuePriority.High, IssueStatus.InProgress, currentUserId, currentUserId, 0, now.AddDays(-2), null, labelCritical);
        var s2Issue5 = CreateIssue(project.Id, sprint2.Id, 9, "Interactive comment deletion permissions", "Only comment authors or project Admins should see the delete trash icon.", IssueType.Task, IssuePriority.Medium, IssueStatus.InProgress, elena.Id, currentUserId, 1, now.AddDays(-2), null, labelBackend);

        // ToDo issues
        var s2Issue6 = CreateIssue(project.Id, sprint2.Id, 10, "Offline queue synchronization banner", "Display non-intrusive badge when changes are pending network sync.", IssueType.Feature, IssuePriority.Low, IssueStatus.ToDo, marcus.Id, currentUserId, 0, now.AddDays(-1), null, labelDesign);
        var s2Issue7 = CreateIssue(project.Id, sprint2.Id, 11, "Haptic feedback on card drop", "Trigger subtle tactile vibration when an issue card snaps into a new column.", IssueType.Task, IssuePriority.Low, IssueStatus.ToDo, null, currentUserId, 1, now.AddDays(-1), null, labelFrontend);

        project.Issues.Add(s2Issue1);
        project.Issues.Add(s2Issue2);
        project.Issues.Add(s2Issue3);
        project.Issues.Add(s2Issue4);
        project.Issues.Add(s2Issue5);
        project.Issues.Add(s2Issue6);
        project.Issues.Add(s2Issue7);

        // 9. Add Backlog Issues (SprintId = null)
        var bIssue1 = CreateIssue(project.Id, null, 12, "Push notifications on @mention in comments", "Deliver instant mobile notification when teammate tags an assignee.", IssueType.Feature, IssuePriority.Medium, IssueStatus.ToDo, null, currentUserId, 0, now, null, labelBackend);
        var bIssue2 = CreateIssue(project.Id, null, 13, "Export sprint burndown to PDF", "Generate printable summary report for hackathon judges.", IssueType.Feature, IssuePriority.Low, IssueStatus.ToDo, null, currentUserId, 1, now, null, labelDesign);
        var bIssue3 = CreateIssue(project.Id, null, 14, "Dark mode high-contrast color polish", "Audit WCAG AA compliance across status chips and priority tags.", IssueType.Task, IssuePriority.Low, IssueStatus.ToDo, null, currentUserId, 2, now, null, labelDesign);

        project.Issues.Add(bIssue1);
        project.Issues.Add(bIssue2);
        project.Issues.Add(bIssue3);

        // 10. Add Comments
        s2Issue4.Comments.Add(new Comment
        {
            Id = Guid.NewGuid(),
            IssueId = s2Issue4.Id,
            AuthorId = sarah.Id,
            Body = "I verified the optimistic update rollback using TanStack Query context snapshotting. Working smoothly!",
            CreatedAt = now.AddDays(-1)
        });

        s2Issue4.Comments.Add(new Comment
        {
            Id = Guid.NewGuid(),
            IssueId = s2Issue4.Id,
            AuthorId = currentUserId,
            Body = "Great work Sarah! Let's get this merged into main before the demo presentation.",
            CreatedAt = now.AddHours(-4)
        });

        _dbContext.Projects.Add(project);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new ProjectDto(
            project.Id,
            project.Name,
            project.Key,
            project.Description,
            project.OwnerId,
            project.CreatedAt,
            ProjectRole.Admin
        );
    }

    private async Task<User> GetOrCreateUserAsync(string name, string email, CancellationToken ct)
    {
        var existing = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == email, ct);
        if (existing is not null) return existing;

        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = name,
            Email = email,
            PasswordHash = "DEMO_USER_HASH",
            CreatedAt = DateTime.UtcNow.AddDays(-30)
        };
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync(ct);
        return user;
    }

    private static Issue CreateIssue(
        Guid projectId,
        Guid? sprintId,
        int number,
        string title,
        string description,
        IssueType type,
        IssuePriority priority,
        IssueStatus status,
        Guid? assigneeId,
        Guid reporterId,
        int boardOrder,
        DateTime createdAt,
        DateTime? completedAt,
        params Label[] labels)
    {
        var issue = new Issue
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            SprintId = sprintId,
            Number = number,
            Title = title,
            Description = description,
            Type = type,
            Priority = priority,
            Status = status,
            AssigneeId = assigneeId,
            ReporterId = reporterId,
            BoardOrder = boardOrder,
            CreatedAt = createdAt,
            UpdatedAt = completedAt ?? createdAt,
            CompletedAt = completedAt
        };

        foreach (var l in labels)
        {
            issue.IssueLabels.Add(new IssueLabel
            {
                IssueId = issue.Id,
                LabelId = l.Id
            });
        }

        return issue;
    }
}
