using Microsoft.AspNetCore.SignalR;

namespace SClinic.Hubs;

/// <summary>
/// ClinicHub — real-time notifications among clinic roles.
/// No DB writes. Events are fire-and-forget Toasts only.
/// </summary>
public class ClinicHub : Hub
{
    // Clients call this to join their role group (called once on connect from layout)
    public async Task JoinRole(string role)
    {
        if (!string.IsNullOrWhiteSpace(role))
            await Groups.AddToGroupAsync(Context.ConnectionId, role);
    }
}
