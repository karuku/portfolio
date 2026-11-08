using TaskApi.Domain;
using TaskApi.Application;
using TaskApi.Infrastructure;

namespace TaskApi.Tests;

public sealed class TaskItemTests
{
    [Fact]
    public void Create_trims_title_and_starts_incomplete()
    {
        var item = TaskItem.Create("  Ship it  ");

        Assert.Equal("Ship it", item.Title);
        Assert.False(item.IsComplete);
        Assert.NotEqual(Guid.Empty, item.Id);
    }

    [Fact]
    public void Complete_is_idempotent()
    {
        var item = TaskItem.Create("Document decisions");

        item.Complete();
        item.Complete();

        Assert.True(item.IsComplete);
    }

    [Fact]
    public async Task Handler_rejects_blank_and_overlong_titles()
    {
        var handler = new CreateTaskHandler(new InMemoryTaskRepository());

        await Assert.ThrowsAsync<ValidationException>(() => handler.HandleAsync(new CreateTaskRequest(" "), CancellationToken.None));
        await Assert.ThrowsAsync<ValidationException>(() => handler.HandleAsync(new CreateTaskRequest(new string('x', 201)), CancellationToken.None));
    }
}
