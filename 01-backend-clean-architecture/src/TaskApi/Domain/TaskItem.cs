namespace TaskApi.Domain;

public sealed class TaskItem
{
    private TaskItem(Guid id, string title, DateTimeOffset createdAt)
    {
        Id = id;
        Title = title;
        CreatedAt = createdAt;
    }

    public Guid Id { get; }
    public string Title { get; }
    public bool IsComplete { get; private set; }
    public DateTimeOffset CreatedAt { get; }

    public static TaskItem Create(string title) =>
        new(Guid.NewGuid(), title.Trim(), DateTimeOffset.UtcNow);

    public void Complete() => IsComplete = true;
}
