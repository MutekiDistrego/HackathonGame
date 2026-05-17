-- ============================================================
-- Міграція: додаємо таблицю round_history для ML-сервісу
-- База: hackathon_sessions (Project 1)
-- ============================================================

-- 1. Таблиця для логування реальних тривалостей раундів
CREATE TABLE IF NOT EXISTS round_history (
    id                       BIGSERIAL PRIMARY KEY,
    session_id               BIGINT       NOT NULL REFERENCES sessions(id) ON DELETE CASCADE,
    round_number             INTEGER      NOT NULL CHECK (round_number BETWEEN 1 AND 10),
    team_count               INTEGER      NOT NULL,
    track                    VARCHAR(1),                  -- NULL якщо треків кілька
    started_at               TIMESTAMP    NOT NULL,
    ended_at                 TIMESTAMP,                   -- NULL поки раунд іде
    actual_duration_minutes  NUMERIC(5,1),                -- рахується при ended_at
    planned_duration_minutes INTEGER,                     -- що задав адмін
    created_at               TIMESTAMP    NOT NULL DEFAULT NOW()
);

-- Індекси для швидких запитів ML-сервісу
CREATE INDEX IF NOT EXISTS idx_round_history_session
    ON round_history(session_id);

CREATE INDEX IF NOT EXISTS idx_round_history_ml
    ON round_history(round_number, team_count, track)
    WHERE actual_duration_minutes IS NOT NULL;

-- ============================================================
-- Для EF Core міграції — додати в SessionDbContext:
-- public DbSet<RoundHistory> RoundHistory { get; set; }
-- ============================================================
