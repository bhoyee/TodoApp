using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TodoApp.Application.Tasks.Queries;
using TodoApp.Domain.Projects;
using TodoApp.Domain.Tasks;
using TodoApp.Infrastructure.Persistence;
using TodoApp.Infrastructure.Persistence.Repositories;

namespace TodoApp.Infrastructure.IntegrationTests.Persistence;

public sealed class RepositoryTests
{
    [Fact]
    public async Task ProjectRepository_AddAndGet_RoundTripsProject()
    {
        await using var database = await TestDatabase.CreateAsync();
        var project = Project.Create(Guid.NewGuid(), "Portfolio launch");

        await using (var writeContext = database.CreateContext())
        {
            var repository = new ProjectRepository(writeContext);
            await repository.AddAsync(project, CancellationToken.None);
            await writeContext.SaveChangesAsync();
        }

        await using var readContext = database.CreateContext();
        var reloaded = await new ProjectRepository(readContext)
            .GetByIdAsync(project.Id, CancellationToken.None);

        Assert.NotNull(reloaded);
        Assert.Equal(project.Name, reloaded.Name);
    }

    [Fact]
    public async Task TaskRepository_LoadedAggregate_TracksAndPersistsMutation()
    {
        await using var database = await TestDatabase.CreateAsync();
        var project = Project.Create(Guid.NewGuid(), "Portfolio launch");
        var task = TaskItem.Create(
            Guid.NewGuid(),
            project.Id,
            "Publish case study");

        await using (var seedContext = database.CreateContext())
        {
            seedContext.AddRange(project, task);
            await seedContext.SaveChangesAsync();
        }

        await using (var updateContext = database.CreateContext())
        {
            var repository = new TaskRepository(updateContext);
            var tracked = await repository.GetByIdAsync(
                task.Id,
                CancellationToken.None);
            tracked!.MoveToReady();
            await updateContext.SaveChangesAsync();
        }

        await using var readContext = database.CreateContext();
        var reloaded = await readContext.Tasks
            .AsNoTracking()
            .SingleAsync();

        Assert.Equal(TaskItemStatus.Ready, reloaded.Status);
    }

    [Fact]
    public async Task Search_AppliesFiltersPrioritySortAndPagination()
    {
        await using var database = await TestDatabase.CreateAsync();
        var project = Project.Create(Guid.NewGuid(), "Portfolio launch");
        var critical = CreateReadyTask(
            project.Id,
            "Critical release",
            PlanningFactors.Create(5, 5, 5, 2));
        var low = CreateReadyTask(
            project.Id,
            "Low-risk release",
            PlanningFactors.Create(1, 1, 1, 8));
        var unrelated = CreateReadyTask(
            project.Id,
            "Write documentation",
            PlanningFactors.Create(5, 5, 5, 1));

        await using (var seedContext = database.CreateContext())
        {
            seedContext.AddRange(project, critical, low, unrelated);
            await seedContext.SaveChangesAsync();
        }

        await using var queryContext = database.CreateContext();
        var repository = new TaskRepository(queryContext);
        var result = await repository.SearchAsync(
            new TaskSearchCriteria(
                project.Id,
                TaskItemStatus.Ready,
                IsBlocked: false,
                Search: "release",
                TaskSortBy.PriorityDescending,
                PageNumber: 1,
                PageSize: 1),
            CancellationToken.None);

        Assert.Equal(2, result.TotalCount);
        var item = Assert.Single(result.Items);
        Assert.Equal(critical.Id, item.Id);
    }

    [Fact]
    public async Task Transaction_WhenRolledBack_DoesNotPersistChanges()
    {
        await using var database = await TestDatabase.CreateAsync();

        await using (var context = database.CreateContext())
        {
            await using var transaction =
                await context.Database.BeginTransactionAsync();
            context.Projects.Add(
                Project.Create(Guid.NewGuid(), "Temporary project"));
            await context.SaveChangesAsync();
            await transaction.RollbackAsync();
        }

        await using var readContext = database.CreateContext();
        Assert.Equal(0, await readContext.Projects.CountAsync());
    }

    private static TaskItem CreateReadyTask(
        Guid projectId,
        string title,
        PlanningFactors planningFactors)
    {
        var task = TaskItem.Create(Guid.NewGuid(), projectId, title);
        task.SetPlanningFactors(planningFactors);
        task.MoveToReady();
        return task;
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
