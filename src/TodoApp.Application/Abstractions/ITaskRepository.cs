using TodoApp.Domain.Tasks;

namespace TodoApp.Application.Abstractions;

public interface ITaskRepository
{
    Task AddAsync(TaskItem task, CancellationToken cancellationToken);
}
