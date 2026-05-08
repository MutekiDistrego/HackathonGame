using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HackathonGame.SessionService.Data;
using HackathonGame.SessionService.DTOs;
using HackathonGame.SessionService.Models;

namespace HackathonGame.SessionService.Controllers;

[ApiController]
[Route("api/sessions/{code}/rounds")]
public class RoundsController : ControllerBase
{
    private readonly SessionDbContext _db;

    public RoundsController(SessionDbContext db) => _db = db;

    // GET /api/sessions/{code}/rounds
    [HttpGet]
    public async Task<ActionResult<List<RoundSettingResponse>>> GetRounds(string code)
    {
        var session = await _db.Sessions
            .Include(s => s.RoundSettings)
            .FirstOrDefaultAsync(s => s.Code == code);

        if (session == null) return NotFound();

        return Ok(session.RoundSettings
            .OrderBy(r => r.RoundNumber)
            .Select(r => new RoundSettingResponse { Id = r.Id, RoundNumber = r.RoundNumber, DurationMinutes = r.DurationMinutes, Name = r.Name })
            .ToList());
    }

    // PUT /api/sessions/{code}/rounds/{roundNumber}
    [HttpPut("{roundNumber}")]
    public async Task<ActionResult> UpdateRound(string code, int roundNumber,
        [FromBody] RoundSettingResponse request)
    {
        var session = await _db.Sessions.FirstOrDefaultAsync(s => s.Code == code);
        if (session == null) return NotFound();

        var round = await _db.RoundSettings
            .FirstOrDefaultAsync(r => r.SessionId == session.Id && r.RoundNumber == roundNumber);
        if (round == null) return NotFound();

        round.DurationMinutes = request.DurationMinutes;
        if (request.Name != null) round.Name = request.Name;
        await _db.SaveChangesAsync();

        return Ok(new RoundSettingResponse { Id = round.Id, RoundNumber = round.RoundNumber, DurationMinutes = round.DurationMinutes, Name = round.Name });
    }
}
