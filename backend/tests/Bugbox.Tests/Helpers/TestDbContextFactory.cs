using Bugbox.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Bugbox.Tests.Helpers;

public static class TestDbContextFactory
{
    public static BugboxDbContext Create(string? dbName = null)
    {
        var options = new DbContextOptionsBuilder<BugboxDbContext>()
            .UseInMemoryDatabase(databaseName: dbName ?? Guid.NewGuid().ToString())
            .Options;

        return new BugboxDbContext(options);
    }
}
