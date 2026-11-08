using TaskApi.Domain;

namespace TaskApi.Application;

public sealed record CreateTaskRequest(string Title);
public sealed record TaskResponse(Guid Id, string Title, bool IsComplete, DateTimeOffset CreatedAt);

public interface ITaskRepository
{
    Task AddAsync(TaskItem item, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<TaskResponse>> ListAsync(CancellationToken cancellationToken);
}

public sealed class CreateTaskHandler(ITaskRepository repository)
{
    public async Task<TaskResponse> HandleAsync(CreateTaskRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            throw new ValidationException("Title is required.");
        if (request.Title.Trim().Length > 200)
            throw new ValidationException("Title must be 200 characters or fewer.");

        var item = TaskItem.Create(request.Title);
        await repository.AddAsync(item, cancellationToken);
        return item.ToResponse();
    }
}

public sealed class ValidationException(string message) : Exception(message);

internal static class TaskMappings
{
    public static TaskResponse ToResponse(this TaskItem item) =>
        new(item.Id, item.Title, item.IsComplete, item.CreatedAt);
}
