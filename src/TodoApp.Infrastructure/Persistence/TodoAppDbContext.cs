using Microsoft.EntityFrameworkCore;
using TodoApp.Application.Abstractions;
using TodoApp.Domain.Projects;
using TodoApp.Domain.Tasks;

namespace TodoApp.Infrastructure.Persistence;

public sealed class TodoAppDbContext(
    DbContextOptions<TodoAppDbContext> options)
    : DbContext(options), IUnitOfWork
{
    public DbSet<Project> Projects => Set<Project>();

    public DbSet<TaskItem> Tasks => Set<TaskItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(TodoAppDbContext).Assembly);
    }

    async Task IUnitOfWork.SaveChangesAsync(
        CancellationToken cancellationToken)
    {
        await SaveChangesAsync(cancellationToken);
    }
}
