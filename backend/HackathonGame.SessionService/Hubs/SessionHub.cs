using Microsoft.AspNetCore.SignalR;

namespace HackathonGame.SessionService.Hubs;

public class SessionHub : Hub
{
    // Client joins a session group by code
    public async Task JoinSession(string sessionCode)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, sessionCode);
        await Clients.Group(sessionCode).SendAsync("UserJoined", Context.ConnectionId);
    }

    // Client leaves a session group
    public async Task LeaveSession(string sessionCode)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, sessionCode);
    }

    // Admin sends announcement to all in session
    public async Task SendAnnouncement(string sessionCode, string message)
    {
        await Clients.Group(sessionCode).SendAsync("Announcement", message);
    }

    // Timer tick — sent from server-side TimerService
    // Client events that the hub can broadcast:
    // - "SessionUpdated" (status, round changes)
    // - "TeamRegistered" (new team joined)
    // - "TimerTick" (remaining seconds)
    // - "RoundStarted" (round info)
    // - "RoundEnded" (round number)
    // - "Announcement" (admin message)

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await base.OnDisconnectedAsync(exception);
    }
}
