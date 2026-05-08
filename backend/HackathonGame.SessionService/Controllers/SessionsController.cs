using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HackathonGame.SessionService.Data;
using HackathonGame.SessionService.DTOs;
using HackathonGame.SessionService.Models;

namespace HackathonGame.SessionService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SessionsController : ControllerBase
{
    private readonly SessionDbContext _db;

    public SessionsController(SessionDbContext db) => _db = db;

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

        // Create round settings
        for (int i = 1; i <= request.TotalRounds; i++)
        {
            _db.RoundSettings.Add(new RoundSetting
            {
                SessionId = session.Id,
                RoundNumber = i,
                DurationMinutes = request.DefaultRoundDuration,
                Name = $"Раунд {i}"
            });
        }
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

    // GET /api/sessions/{code}/state
    [HttpGet("{code}/state")]
    public async Task<ActionResult<SessionStateResponse>> GetSessionState(string code)
    {
        var session = await _db.Sessions
            .Include(s => s.Teams).ThenInclude(t => t.Members)
            .FirstOrDefaultAsync(s => s.Code == code);

        if (session == null) return NotFound(new { message = "Сесію не знайдено" });

        long? remaining = null;
        if (session.RoundEndTime.HasValue && session.Status == "ACTIVE")
        {
            remaining = Math.Max(0, (long)(session.RoundEndTime.Value - DateTime.UtcNow).TotalSeconds);
        }

        return Ok(new SessionStateResponse
        {
            Code = session.Code, Status = session.Status,
            CurrentRound = session.CurrentRound, RoundEndTime = session.RoundEndTime,
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
        if (!valid.Contains(request.Status))
            return BadRequest(new { message = "Невалідний статус" });

        session.Status = request.Status;
        if (request.Status == "FINISHED")
            session.GameEndTime = DateTime.UtcNow;

        await _db.SaveChangesAsync();
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

        var roundSetting = session.RoundSettings
            .FirstOrDefault(r => r.RoundNumber == session.CurrentRound);
        var duration = roundSetting?.DurationMinutes ?? 15;

        session.Status = "ACTIVE";
        session.RoundEndTime = DateTime.UtcNow.AddMinutes(duration);
        await _db.SaveChangesAsync();

        return Ok(new
        {
            round = session.CurrentRound,
            roundEndTime = session.RoundEndTime,
            durationMinutes = duration
        });
    }

    // POST /api/sessions/{code}/rounds/pause
    [HttpPost("{code}/rounds/pause")]
    public async Task<ActionResult> PauseRound(string code)
    {
        var session = await _db.Sessions.FirstOrDefaultAsync(s => s.Code == code);
        if (session == null) return NotFound();

        session.Status = "PAUSED";
        await _db.SaveChangesAsync();
        return Ok(new { status = "PAUSED" });
    }

    // POST /api/sessions/{code}/rounds/next
    [HttpPost("{code}/rounds/next")]
    public async Task<ActionResult> NextRound(string code)
    {
        var session = await _db.Sessions
            .Include(s => s.RoundSettings)
            .FirstOrDefaultAsync(s => s.Code == code);
        if (session == null) return NotFound();

        session.CurrentRound++;
        session.Status = "WAITING";
        session.RoundEndTime = null;
        await _db.SaveChangesAsync();

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

    // --- Helpers ---

    private async Task<Session?> GetSessionWithIncludes(string code) =>
        await _db.Sessions
            .Include(s => s.Teams).ThenInclude(t => t.Members)
            .Include(s => s.RoundSettings)
            .FirstOrDefaultAsync(s => s.Code == code);

    private static string GenerateCode()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        var random = new Random();
        return new string(Enumerable.Range(0, 6).Select(_ => chars[random.Next(chars.Length)]).ToArray());
    }

    private static SessionResponse MapToResponse(Session s) => new()
    {
        Id = s.Id, Code = s.Code, Name = s.Name, Status = s.Status,
        CurrentRound = s.CurrentRound, RoundEndTime = s.RoundEndTime,
        GameEndTime = s.GameEndTime, CreatedAt = s.CreatedAt,
        TeamCount = s.Teams.Count,
        RoundSettings = s.RoundSettings.OrderBy(r => r.RoundNumber).Select(r =>
            new RoundSettingResponse { Id = r.Id, RoundNumber = r.RoundNumber, DurationMinutes = r.DurationMinutes, Name = r.Name }
        ).ToList()
    };

    private static TeamResponse MapTeam(Team t) => new()
    {
        Id = t.Id, Name = t.Name, LifeTokens = t.LifeTokens,
        SelectedTrack = t.SelectedTrack, CreatedAt = t.CreatedAt,
        Members = t.Members.Select(m => new TeamMemberResponse { Id = m.Id, Name = m.Name, Role = m.Role }).ToList()
    };
}
