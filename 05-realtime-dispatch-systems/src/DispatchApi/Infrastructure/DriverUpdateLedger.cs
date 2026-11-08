using DispatchApi.Domain;

namespace DispatchApi.Infrastructure;

public sealed class DriverUpdateLedger
{
    private const int MaximumRecentEvents = 500;
    private readonly object _gate = new();
    private readonly Dictionary<string, DriverUpdate> _eventsById = new(StringComparer.Ordinal);
    private readonly Queue<string> _eventOrder = new();

    public UpdateReceipt Record(DriverUpdate update)
    {
        lock (_gate)
        {
            if (_eventsById.TryGetValue(update.Id, out var existing))
            {
                return existing == update
                    ? new UpdateReceipt(UpdateReceiptStatus.Replay, existing)
                    : new UpdateReceipt(UpdateReceiptStatus.Conflict, existing);
            }

            _eventsById.Add(update.Id, update);
            _eventOrder.Enqueue(update.Id);
            while (_eventOrder.Count > MaximumRecentEvents)
                _eventsById.Remove(_eventOrder.Dequeue());

            return new UpdateReceipt(UpdateReceiptStatus.Created, update);
        }
    }

    public DriverUpdate? Find(string id)
    {
        lock (_gate)
            return _eventsById.GetValueOrDefault(id);
    }

    public IReadOnlyCollection<DriverUpdate> ListRecent()
    {
        lock (_gate)
            return _eventOrder.Reverse().Select(id => _eventsById[id]).ToArray();
    }
}
