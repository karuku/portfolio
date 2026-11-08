using System.Collections.Concurrent;
using TaskApi.Application;
using TaskApi.Domain;

namespace TaskApi.Infrastructure;

public sealed class InMemoryTaskRepository : ITaskRepository
{
    private readonly ConcurrentDictionary<Guid, TaskItem> items = new();

    public Task AddAsync(TaskItem item, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        items[item.Id] = item;
        return Task.CompletedTask;
    }

    public Task<IReadOnlyCollection<TaskResponse>> ListAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        IReadOnlyCollection<TaskResponse> result = items.Values
            .OrderByDescending(item => item.CreatedAt)
            .Select(item => new TaskResponse(item.Id, item.Title, item.IsComplete, item.CreatedAt))
            .ToArray();
        return Task.FromResult(result);
    }
}
