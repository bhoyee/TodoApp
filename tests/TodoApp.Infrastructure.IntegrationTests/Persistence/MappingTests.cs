using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TodoApp.Domain.Projects;
using TodoApp.Domain.Tasks;
using TodoApp.Infrastructure.Persistence;

namespace TodoApp.Infrastructure.IntegrationTests.Persistence;

public sealed class MappingTests
{
    [Fact]
    public async Task Project_RoundTrip_PreservesDetailsAndArchiveState()
    {
        await using var database = await TestDatabase.CreateAsync();
        var archivedAt =
            new DateTimeOffset(2026, 7, 3, 9, 30, 0, TimeSpan.Zero);
        var project = Project.Create(
            Guid.NewGuid(),
            "Portfolio launch",
            "Public release");
        project.SetTargetDate(
            DueDate.Create(new DateOnly(2026, 8, 31)));
        project.Archive(archivedAt);

        await using (var writeContext = database.CreateContext())
        {
            writeContext.Projects.Add(project);
            await writeContext.SaveChangesAsync();
        }

        await using var readContext = database.CreateContext();
        var reloaded = await readContext.Projects
            .AsNoTracking()
            .SingleAsync();

        Assert.Equal(project.Id, reloaded.Id);
        Assert.Equal("Portfolio launch", reloaded.Name);
        Assert.Equal("Public release", reloaded.Description);
        Assert.Equal(new DateOnly(2026, 8, 31), reloaded.TargetDate?.Value);
        Assert.Equal(archivedAt, reloaded.ArchivedAt);
    }

    [Fact]
    public async Task Task_RoundTrip_PreservesLifecycleAndPlanningValues()
    {
        await using var database = await TestDatabase.CreateAsync();
        var project = Project.Create(Guid.NewGuid(), "Portfolio launch");
        var completedAt =
            new DateTimeOffset(2026, 7, 3, 10, 0, 0, TimeSpan.Zero);
        var task = TaskItem.Create(
            Guid.NewGuid(),
            project.Id,
            "Publish case study");
        task.Schedule(DueDate.Create(new DateOnly(2026, 8, 1)));
        task.Estimate(EffortEstimate.Create(5));
        task.SetPlanningFactors(
            PlanningFactors.Create(5, 4, 3, 2));
        task.MoveToReady();
        task.Start();
        task.Complete(completedAt);

        await using (var writeContext = database.CreateContext())
        {
            writeContext.Projects.Add(project);
            writeContext.Tasks.Add(task);
            await writeContext.SaveChangesAsync();
        }

        await using var readContext = database.CreateContext();
        var reloaded = await readContext.Tasks
            .AsNoTracking()
            .SingleAsync();

        Assert.Equal(task.Id, reloaded.Id);
        Assert.Equal(project.Id, reloaded.ProjectId);
        Assert.Equal("Publish case study", reloaded.Title);
        Assert.Equal(TaskItemStatus.Completed, reloaded.Status);
        Assert.Equal(completedAt, reloaded.CompletedAt);
        Assert.Equal(new DateOnly(2026, 8, 1), reloaded.DueDate?.Value);
        Assert.Equal(5, reloaded.EffortEstimate?.Value);
        Assert.Equal(14.5m, reloaded.Priority.Value);
    }

    private sealed class TestDatabase(
        SqliteConnection connection,
        DbContextOptions<TodoAppDbContext> options)
        : IAsyncDisposable
    {
        public static async Task<TestDatabase> CreateAsync()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var options = new DbContextOptionsBuilder<TodoAppDbContext>()
                .UseSqlite(connection)
                .Options;
            var database = new TestDatabase(connection, options);

            await using var context = database.CreateContext();
            await context.Database.EnsureCreatedAsync();
            return database;
        }

        public TodoAppDbContext CreateContext() => new(options);

        public ValueTask DisposeAsync() => connection.DisposeAsync();
    }
}
