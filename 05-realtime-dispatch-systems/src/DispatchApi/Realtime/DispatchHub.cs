using DispatchApi.Domain;
using Microsoft.AspNetCore.SignalR;

namespace DispatchApi.Realtime;

public interface IDispatchClient
{
    Task AssignmentCreated(DispatchAssignmentResponse assignment);
    Task DriverUpdateReceived(DriverUpdateResponse update);
}

public sealed class DispatchHub : Hub<IDispatchClient>
{
}
