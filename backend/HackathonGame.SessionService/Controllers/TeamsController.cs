using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HackathonGame.SessionService.Data;
using HackathonGame.SessionService.DTOs;
using HackathonGame.SessionService.Models;

namespace HackathonGame.SessionService.Controllers;

[ApiController]
[Route("api/sessions/{code}/teams")]
public class TeamsController : ControllerBase
{
    private readonly SessionDbContext _db;

    public TeamsController(SessionDbContext db) => _db = db;

    // POST /api/sessions/{code}/teams
    [HttpPost]
    public async Task<ActionResult<TeamResponse>> RegisterTeam(string code, RegisterTeamRequest request)
    {
        var session = await _db.Sessions.FirstOrDefaultAsync(s => s.Code == code);
        if (session == null) return NotFound(new { message = "Сесію не знайдено" });

        var team = new Team
        {
            SessionId = session.Id,
            Name = request.Name,
            LifeTokens = 3
        };
        _db.Teams.Add(team);
        await _db.SaveChangesAsync();

        if (request.Members != null)
        {
            foreach (var m in request.Members)
            {
                _db.TeamMembers.Add(new TeamMember
                {
                    TeamId = team.Id,
                    Name = m.Name,
                    Role = m.Role
                });
            }
            await _db.SaveChangesAsync();
        }

        var created = await _db.Teams
            .Include(t => t.Members)
            .FirstAsync(t => t.Id == team.Id);

        return CreatedAtAction(nameof(GetTeams), new { code }, MapTeam(created));
    }

    // GET /api/sessions/{code}/teams
    [HttpGet]
    public async Task<ActionResult<List<TeamResponse>>> GetTeams(string code)
    {
        var session = await _db.Sessions.FirstOrDefaultAsync(s => s.Code == code);
        if (session == null) return NotFound(new { message = "Сесію не знайдено" });

        var teams = await _db.Teams
            .Include(t => t.Members)
            .Where(t => t.SessionId == session.Id)
            .OrderBy(t => t.CreatedAt)
            .ToListAsync();

        return Ok(teams.Select(MapTeam).ToList());
    }

    // GET /api/sessions/{code}/teams/{teamId}
    [HttpGet("{teamId}")]
    public async Task<ActionResult<TeamResponse>> GetTeam(string code, long teamId)
    {
        var team = await _db.Teams
            .Include(t => t.Members)
            .FirstOrDefaultAsync(t => t.Id == teamId);
        if (team == null) return NotFound();
        return Ok(MapTeam(team));
    }

    // DELETE /api/sessions/{code}/teams/{teamId}
    [HttpDelete("{teamId}")]
    public async Task<ActionResult> DeleteTeam(string code, long teamId)
    {
        var team = await _db.Teams
            .Include(t => t.Members)
            .FirstOrDefaultAsync(t => t.Id == teamId);
        if (team == null) return NotFound();

        _db.TeamMembers.RemoveRange(team.Members);
        _db.Teams.Remove(team);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // PUT /api/sessions/{code}/teams/{teamId}/track
    [HttpPut("{teamId}/track")]
    public async Task<ActionResult> SelectTrack(string code, long teamId, [FromBody] string track)
    {
        var team = await _db.Teams.FindAsync(teamId);
        if (team == null) return NotFound();

        team.SelectedTrack = track;
        await _db.SaveChangesAsync();
        return Ok(new { selectedTrack = team.SelectedTrack });
    }

    // PUT /api/sessions/{code}/teams/{teamId}/tokens
    [HttpPut("{teamId}/tokens")]
    public async Task<ActionResult> UpdateTokens(string code, long teamId, [FromBody] int tokens)
    {
        var team = await _db.Teams.FindAsync(teamId);
        if (team == null) return NotFound();

        team.LifeTokens = tokens;
        await _db.SaveChangesAsync();
        return Ok(new { lifeTokens = team.LifeTokens });
    }

    private static TeamResponse MapTeam(Team t) => new()
    {
        Id = t.Id, Name = t.Name, LifeTokens = t.LifeTokens,
        SelectedTrack = t.SelectedTrack, CreatedAt = t.CreatedAt,
        Members = t.Members.Select(m => new TeamMemberResponse { Id = m.Id, Name = m.Name, Role = m.Role }).ToList()
    };
}
