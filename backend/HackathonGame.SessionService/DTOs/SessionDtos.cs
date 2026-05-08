namespace HackathonGame.SessionService.DTOs;

// --- Request DTOs ---

public class CreateSessionRequest
{
    public string Name { get; set; } = string.Empty;
    public string AdminPassword { get; set; } = string.Empty;
    public int TotalRounds { get; set; } = 3;
    public int DefaultRoundDuration { get; set; } = 15;
}

public class UpdateStatusRequest
{
    public string Status { get; set; } = string.Empty;
}

public class AdjustTimeRequest
{
    public int Minutes { get; set; }
}

public class RegisterTeamRequest
{
    public string Name { get; set; } = string.Empty;
    public List<TeamMemberDto>? Members { get; set; }
}

public class TeamMemberDto
{
    public string Name { get; set; } = string.Empty;
    public string? Role { get; set; }
}

// --- Response DTOs ---

public class SessionResponse
{
    public long Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int CurrentRound { get; set; }
    public DateTime? RoundEndTime { get; set; }
    public DateTime? GameEndTime { get; set; }
    public DateTime CreatedAt { get; set; }
    public int TeamCount { get; set; }
    public List<RoundSettingResponse>? RoundSettings { get; set; }
}

public class SessionStateResponse
{
    public string Code { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int CurrentRound { get; set; }
    public DateTime? RoundEndTime { get; set; }
    public long? RemainingSeconds { get; set; }
    public List<TeamResponse> Teams { get; set; } = new();
}

public class TeamResponse
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int LifeTokens { get; set; }
    public string? SelectedTrack { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<TeamMemberResponse> Members { get; set; } = new();
}

public class TeamMemberResponse
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Role { get; set; }
}

public class RoundSettingResponse
{
    public long Id { get; set; }
    public int RoundNumber { get; set; }
    public int DurationMinutes { get; set; }
    public string? Name { get; set; }
}
