using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using HackathonGame.SessionService.Data;
using HackathonGame.SessionService.Hubs;

namespace HackathonGame.SessionService.Services;

public class TimerBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHubContext<SessionHub> _hubContext;
    private readonly ILogger<TimerBackgroundService> _logger;

    public TimerBackgroundService(
        IServiceScopeFactory scopeFactory,
        IHubContext<SessionHub> hubContext,
        ILogger<TimerBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _hubContext = hubContext;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("TimerBackgroundService started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            await TickAsync();
            await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
        }
    }

    private async Task TickAsync()
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<SessionDbContext>();

            var activeSessions = await db.Sessions
                .Where(s => s.Status == "ACTIVE" && s.RoundEndTime != null)
                .ToListAsync();

            foreach (var session in activeSessions)
            {
                var remaining = (long)Math.Max(0,
                    (session.RoundEndTime!.Value - DateTime.UtcNow).TotalSeconds);

                // Broadcast timer tick to all clients in the session group
                await _hubContext.Clients
                    .Group(session.Code)
                    .SendAsync("TimerTick", new { remaining, round = session.CurrentRound });

                // Round expired — mark as WAITING and notify
                if (remaining == 0)
                {
                    session.Status = "WAITING";
                    session.RoundEndTime = null;
                    await db.SaveChangesAsync();

                    await _hubContext.Clients
                        .Group(session.Code)
                        .SendAsync("RoundEnded", new { round = session.CurrentRound });

                    _logger.LogInformation("Round {Round} ended for session {Code}.",
                        session.CurrentRound, session.Code);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in TimerBackgroundService tick.");
        }
    }
}