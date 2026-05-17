using System.Text.Json;
using HackathonGame.SessionService.DTOs;

namespace HackathonGame.SessionService.Services;

/// <summary>
/// HTTP-клієнт до Python FastAPI ML-сервісу (порт 8084).
/// Якщо ML-сервіс недоступний — повертає null, контролер
/// автоматично переходить на вбудований fallback.
/// </summary>
public class MlRecommendationService
{
    private readonly HttpClient _http;
    private readonly ILogger<MlRecommendationService> _log;

    private static readonly JsonSerializerOptions _jsonOpts =
        new() { PropertyNameCaseInsensitive = true };

    public MlRecommendationService(
        IHttpClientFactory factory,
        ILogger<MlRecommendationService> log)
    {
        _http = factory.CreateClient("ml");
        _log = log;
    }

    /// <summary>
    /// Отримує рекомендовану тривалість раунду від ML-сервісу.
    /// Повертає null якщо сервіс недоступний (graceful degradation).
    /// </summary>
    public async Task<DurationRecommendationResponse?> GetRecommendationAsync(
        int teams, string track, int round)
    {
        try
        {
            var url = $"recommend-duration?teams={teams}&track={track}&round={round}";
            var resp = await _http.GetAsync(url);
            resp.EnsureSuccessStatusCode();

            var body = await resp.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<DurationRecommendationResponse>(body, _jsonOpts);
        }
        catch (Exception ex)
        {
            _log.LogWarning("ML-сервіс недоступний: {Msg}", ex.Message);
            return null;
        }
    }

    /// <summary>
    /// Запускає перенавчання моделі на свіжих даних.
    /// Fire-and-forget — не блокує основний flow.
    /// </summary>
    public async Task TriggerRetrainAsync()
    {
        try
        {
            await _http.PostAsync("retrain", null);
            _log.LogInformation("ML retrain triggered.");
        }
        catch (Exception ex)
        {
            _log.LogWarning("ML retrain не вдався: {Msg}", ex.Message);
        }
    }
}