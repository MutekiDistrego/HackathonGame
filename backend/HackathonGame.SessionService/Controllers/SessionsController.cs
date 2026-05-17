using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using HackathonGame.SessionService.Data;
using HackathonGame.SessionService.DTOs;
using HackathonGame.SessionService.Hubs;
using HackathonGame.SessionService.Models;
using HackathonGame.SessionService.Services;

namespace HackathonGame.SessionService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SessionsController : ControllerBase
{
    private readonly SessionDbContext _db;
    private readonly IHubContext<SessionHub> _hub;

    public SessionsController(SessionDbContext db, IHubContext<SessionHub> hub)
    {
        _db = db;
        _hub = hub;
    }

    // POST /api/sessions
    [HttpPost]
    public async Task<ActionResult<SessionResponse>> CreateSession(CreateSessionRequest request)
    {
        var code = GenerateCode();
        var session = new Session
        {
            Code = code,
            Name = request.Name,
            AdminPassword = request.AdminPassword,
            Status = "WAITING",
            CurrentRound = 1
        };
        _db.Sessions.Add(session);
        await _db.SaveChangesAsync();

        for (int i = 1; i <= request.TotalRounds; i++)
            _db.RoundSettings.Add(new RoundSetting
            {
                SessionId = session.Id,
                RoundNumber = i,
                DurationMinutes = request.DefaultRoundDuration,
                Name = $"Раунд {i}"
            });
        await _db.SaveChangesAsync();

        var created = await GetSessionWithIncludes(session.Code);
        return CreatedAtAction(nameof(GetSession), new { code = session.Code }, MapToResponse(created!));
    }

    // GET /api/sessions/{code}
    [HttpGet("{code}")]
    public async Task<ActionResult<SessionResponse>> GetSession(string code)
    {
        var session = await GetSessionWithIncludes(code);
        if (session == null) return NotFound(new { message = "Сесію не знайдено" });
        return Ok(MapToResponse(session));
    }

    // DELETE /api/sessions/{code}
    [HttpDelete("{code}")]
    public async Task<ActionResult> DeleteSession(string code)
    {
        var session = await _db.Sessions.FirstOrDefaultAsync(s => s.Code == code);
        if (session == null) return NotFound(new { message = "Сесію не знайдено" });
        _db.Sessions.Remove(session);
        await _db.SaveChangesAsync();
        await _hub.Clients.Group(code).SendAsync("SessionDeleted", new { code });
        return NoContent();
    }

    // GET /api/sessions/{code}/state
    [HttpGet("{code}/state")]
    public async Task<ActionResult<SessionStateResponse>> GetSessionState(string code)
    {
        var session = await _db.Sessions
            .Include(s => s.Teams).ThenInclude(t => t.Members)
            .FirstOrDefaultAsync(s => s.Code == code);
        if (session == null) return NotFound(new { message = "Сесію не знайдено" });

        long? remaining = null;
        if (session.Status == "ACTIVE" && session.RoundEndTime.HasValue)
            remaining = Math.Max(0, (long)(session.RoundEndTime.Value - DateTime.UtcNow).TotalSeconds);
        else if (session.Status == "PAUSED" && session.RoundEndTime.HasValue && session.PausedAt.HasValue)
            remaining = Math.Max(0, (long)(session.RoundEndTime.Value - session.PausedAt.Value).TotalSeconds);

        return Ok(new SessionStateResponse
        {
            Code = session.Code,
            Status = session.Status,
            CurrentRound = session.CurrentRound,
            RoundEndTime = session.RoundEndTime,
            RemainingSeconds = remaining,
            Teams = session.Teams.Select(MapTeam).ToList()
        });
    }

    // PUT /api/sessions/{code}/status
    [HttpPut("{code}/status")]
    public async Task<ActionResult> UpdateStatus(string code, UpdateStatusRequest request)
    {
        var session = await _db.Sessions.FirstOrDefaultAsync(s => s.Code == code);
        if (session == null) return NotFound();
        var valid = new[] { "WAITING", "ACTIVE", "PAUSED", "FINISHED" };
        if (!valid.Contains(request.Status)) return BadRequest(new { message = "Невалідний статус" });
        session.Status = request.Status;
        if (request.Status == "FINISHED") session.GameEndTime = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        await _hub.Clients.Group(code).SendAsync("SessionUpdated", new { status = session.Status });
        return Ok(new { status = session.Status });
    }

    // POST /api/sessions/{code}/rounds/start
    [HttpPost("{code}/rounds/start")]
    public async Task<ActionResult> StartRound(string code)
    {
        var session = await _db.Sessions
            .Include(s => s.RoundSettings)
            .FirstOrDefaultAsync(s => s.Code == code);
        if (session == null) return NotFound();

        var duration = session.RoundSettings
            .FirstOrDefault(r => r.RoundNumber == session.CurrentRound)?.DurationMinutes ?? 15;

        session.Status = "ACTIVE";
        session.RoundEndTime = DateTime.UtcNow.AddMinutes(duration);
        session.PausedAt = null;
        await _db.SaveChangesAsync();

        // ── НОВЕ: логуємо старт раунду в round_history ───────
        await LogRoundStarted(session, duration);

        var payload = new { round = session.CurrentRound, roundEndTime = session.RoundEndTime, durationMinutes = duration };
        await _hub.Clients.Group(code).SendAsync("RoundStarted", payload);
        return Ok(payload);
    }

    // POST /api/sessions/{code}/rounds/pause
    [HttpPost("{code}/rounds/pause")]
    public async Task<ActionResult> PauseRound(string code)
    {
        var session = await _db.Sessions.FirstOrDefaultAsync(s => s.Code == code);
        if (session == null) return NotFound();
        if (session.Status != "ACTIVE") return BadRequest(new { message = "Раунд не активний" });

        session.PausedAt = DateTime.UtcNow;
        session.Status = "PAUSED";
        await _db.SaveChangesAsync();

        var frozen = Math.Max(0, (long)(session.RoundEndTime!.Value - session.PausedAt.Value).TotalSeconds);
        await _hub.Clients.Group(code).SendAsync("RoundPaused", new { status = "PAUSED", remainingSeconds = frozen });
        return Ok(new { status = "PAUSED", remainingSeconds = frozen });
    }

    // POST /api/sessions/{code}/rounds/resume
    [HttpPost("{code}/rounds/resume")]
    public async Task<ActionResult> ResumeRound(string code)
    {
        var session = await _db.Sessions.FirstOrDefaultAsync(s => s.Code == code);
        if (session == null) return NotFound();
        if (session.Status != "PAUSED") return BadRequest(new { message = "Сесія не на паузі" });

        if (session.PausedAt.HasValue && session.RoundEndTime.HasValue)
        {
            var pausedFor = DateTime.UtcNow - session.PausedAt.Value;
            session.RoundEndTime = session.RoundEndTime.Value + pausedFor;
        }

        session.Status = "ACTIVE";
        session.PausedAt = null;
        await _db.SaveChangesAsync();

        await _hub.Clients.Group(code).SendAsync("RoundResumed", new { status = "ACTIVE", roundEndTime = session.RoundEndTime });
        return Ok(new { status = "ACTIVE", roundEndTime = session.RoundEndTime });
    }

    // POST /api/sessions/{code}/rounds/next
    [HttpPost("{code}/rounds/next")]
    public async Task<ActionResult> NextRound(string code)
    {
        var session = await _db.Sessions
            .Include(s => s.RoundSettings)
            .FirstOrDefaultAsync(s => s.Code == code);
        if (session == null) return NotFound();

        // ── НОВЕ: логуємо завершення раунду перед переходом ──
        await LogRoundEnded(session.Id, session.CurrentRound);

        session.CurrentRound++;
        session.Status = "WAITING";
        session.RoundEndTime = null;
        session.PausedAt = null;
        await _db.SaveChangesAsync();

        await _hub.Clients.Group(code).SendAsync("SessionUpdated",
            new { status = "WAITING", currentRound = session.CurrentRound });
        return Ok(new { currentRound = session.CurrentRound });
    }

    // PUT /api/sessions/{code}/rounds/time
    [HttpPut("{code}/rounds/time")]
    public async Task<ActionResult> AdjustTime(string code, AdjustTimeRequest request)
    {
        var session = await _db.Sessions.FirstOrDefaultAsync(s => s.Code == code);
        if (session == null) return NotFound();
        if (session.RoundEndTime.HasValue)
        {
            session.RoundEndTime = session.RoundEndTime.Value.AddMinutes(request.Minutes);
            await _db.SaveChangesAsync();
        }
        return Ok(new { roundEndTime = session.RoundEndTime });
    }

    // ── НОВЕ: GET /api/sessions/recommend-duration ──────────
    /// <summary>
    /// Повертає рекомендовану тривалість раунду від ML-сервісу.
    /// Приклад: GET /api/sessions/recommend-duration?teams=6&track=A&round=3
    /// Якщо ML-сервіс недоступний — повертає вбудований fallback.
    /// </summary>
    [HttpGet("recommend-duration")]
    public async Task<ActionResult<object>> RecommendDuration(
        [FromQuery] int teams,
        [FromQuery] string track,
        [FromQuery] int round,
        [FromServices] MlRecommendationService ml)
    {
        var trackUpper = track.ToUpperInvariant();
        if (!new[] { "A", "B", "C" }.Contains(trackUpper))
            return BadRequest(new { message = "track має бути A, B або C" });

        var rec = await ml.GetRecommendationAsync(teams, trackUpper, round);

        if (rec is null)
        {
            // Проста евристика без ML (використовується коли ML-сервіс down)
            var fallback = 12 + (round - 1) + Math.Max(0, teams - 4);
            return Ok(new
            {
                recommendedMinutes = fallback,
                confidence = 0.0,
                nTrainingSamples = 0,
                trainedAt = (string?)null,
                note = "ml_unavailable_fallback"
            });
        }

        return Ok(rec);
    }

    // ─────────────────────────────────────────────────────────
    // Private helpers
    // ─────────────────────────────────────────────────────────

    /// <summary>
    /// Викликається в кінці StartRound().
    /// Записує новий рядок у round_history зі StartedAt.
    /// </summary>
    private async Task LogRoundStarted(Session session, int plannedMinutes)
    {
        // Визначаємо найпоширеніший трек серед команд сесії
        var track = await _db.Teams
            .Where(t => t.SessionId == session.Id && t.SelectedTrack != null)
            .GroupBy(t => t.SelectedTrack)
            .OrderByDescending(g => g.Count())
            .Select(g => g.Key)
            .FirstOrDefaultAsync();

        var teamCount = await _db.Teams
            .CountAsync(t => t.SessionId == session.Id);

        _db.RoundHistory.Add(new RoundHistory
        {
            SessionId = session.Id,
            RoundNumber = session.CurrentRound,
            TeamCount = teamCount,
            Track = track,
            StartedAt = DateTime.UtcNow,
            PlannedDurationMinutes = plannedMinutes,
        });
        await _db.SaveChangesAsync();
    }

    /// <summary>
    /// Викликається на початку NextRound().
    /// Заповнює EndedAt та ActualDurationMinutes для поточного раунду.
    /// </summary>
    private async Task LogRoundEnded(long sessionId, int roundNumber)
    {
        var history = await _db.RoundHistory
            .Where(r => r.SessionId == sessionId
                     && r.RoundNumber == roundNumber
                     && r.EndedAt == null)
            .OrderByDescending(r => r.StartedAt)
            .FirstOrDefaultAsync();

        if (history is not null)
        {
            history.EndedAt = DateTime.UtcNow;
            history.ActualDurationMinutes =
                (decimal)(history.EndedAt.Value - history.StartedAt).TotalMinutes;
            await _db.SaveChangesAsync();
        }
    }

    private async Task<Session?> GetSessionWithIncludes(string code) =>
        await _db.Sessions
            .Include(s => s.Teams).ThenInclude(t => t.Members)
            .Include(s => s.RoundSettings)
            .FirstOrDefaultAsync(s => s.Code == code);

    private static string GenerateCode()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        var random = new Random();
        return new string(Enumerable.Range(0, 6)
            .Select(_ => chars[random.Next(chars.Length)]).ToArray());
    }

    private static SessionResponse MapToResponse(Session s) => new()
    {
        Id = s.Id,
        Code = s.Code,
        Name = s.Name,
        Status = s.Status,
        CurrentRound = s.CurrentRound,
        RoundEndTime = s.RoundEndTime,
        GameEndTime = s.GameEndTime,
        CreatedAt = s.CreatedAt,
        TeamCount = s.Teams.Count,
        RoundSettings = s.RoundSettings.OrderBy(r => r.RoundNumber).Select(r =>
            new RoundSettingResponse
            {
                Id = r.Id,
                RoundNumber = r.RoundNumber,
                DurationMinutes = r.DurationMinutes,
                Name = r.Name
            }).ToList()
    };

    private static TeamResponse MapTeam(Team t) => new()
    {
        Id = t.Id,
        Name = t.Name,
        LifeTokens = t.LifeTokens,
        SelectedTrack = t.SelectedTrack,
        CreatedAt = t.CreatedAt,
        Members = t.Members.Select(m =>
            new TeamMemberResponse { Id = m.Id, Name = m.Name, Role = m.Role }).ToList()
    };
}