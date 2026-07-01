using TodoApp.Domain.Common;

namespace TodoApp.Domain.Tasks;

public sealed class TaskItem
{
    private TaskItem(Guid id, string title)
    {
        if (id == Guid.Empty)
        {
            throw new DomainValidationException("Task identifier is required.");
        }

        if (string.IsNullOrWhiteSpace(title))
        {
            throw new DomainValidationException("Task title is required.");
        }

        Id = id;
        Title = title.Trim();
        Status = TaskItemStatus.Backlog;
    }

    public Guid Id { get; }

    public string Title { get; private set; }

    public TaskItemStatus Status { get; private set; }

    public string? BlockedReason { get; private set; }

    public DateTimeOffset? CompletedAt { get; private set; }

    public static TaskItem Create(Guid id, string title) => new(id, title);

    public void MoveToReady()
    {
        EnsureStatus(
            TaskItemStatus.Backlog,
            "Only a backlog task can be moved to ready.");

        Status = TaskItemStatus.Ready;
    }

    public void Start()
    {
        EnsureStatus(
            TaskItemStatus.Ready,
            "Only a ready task can be started.");

        Status = TaskItemStatus.InProgress;
    }

    public void Block(string reason)
    {
        EnsureStatus(
            TaskItemStatus.InProgress,
            "Only an in-progress task can be blocked.");

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new DomainValidationException("A blocked reason is required.");
        }

        BlockedReason = reason.Trim();
        Status = TaskItemStatus.Blocked;
    }

    public void Unblock()
    {
        EnsureStatus(
            TaskItemStatus.Blocked,
            "Only a blocked task can be unblocked.");

        BlockedReason = null;
        Status = TaskItemStatus.Ready;
    }

    public void Complete(DateTimeOffset completedAt)
    {
        EnsureStatus(
            TaskItemStatus.InProgress,
            "Only an in-progress task can be completed.");

        CompletedAt = completedAt;
        Status = TaskItemStatus.Completed;
    }

    public void Reopen()
    {
        EnsureStatus(
            TaskItemStatus.Completed,
            "Only a completed task can be reopened.");

        CompletedAt = null;
        Status = TaskItemStatus.Ready;
    }

    private void EnsureStatus(TaskItemStatus requiredStatus, string message)
    {
        if (Status != requiredStatus)
        {
            throw new DomainRuleException(message);
        }
    }
}
