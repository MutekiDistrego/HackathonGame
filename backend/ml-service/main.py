"""
Round Duration Recommendation Service
Запуск: uvicorn main:app --port 8084 --reload

Залежності: pip install fastapi uvicorn scikit-learn pandas numpy psycopg2-binary
"""

from fastapi import FastAPI, HTTPException, Query
from fastapi.middleware.cors import CORSMiddleware
from pydantic import BaseModel
import numpy as np
import pandas as pd
from sklearn.linear_model import RidgeCV
from sklearn.preprocessing import OneHotEncoder
from sklearn.pipeline import Pipeline
from sklearn.compose import ColumnTransformer
import psycopg2
import os
import json
import pickle
from pathlib import Path
from datetime import datetime

app = FastAPI(title="Round Duration ML Service", version="1.0.0")

app.add_middleware(
    CORSMiddleware,
    allow_origins=["http://localhost:8081", "http://localhost:3001", "http://localhost:3004"],
    allow_methods=["*"],
    allow_headers=["*"],
)

# ─── Шлях до збереженої моделі ───────────────────────────────────────────────
MODEL_PATH = Path("model.pkl")
DATA_PATH  = Path("training_data.json")

# ─── Підключення до БД ────────────────────────────────────────────────────────
DB_URL = os.getenv(
    "DATABASE_URL",
    "postgresql://postgres:postgres@localhost:5435/hackathon_sessions"
)

def get_training_data_from_db() -> pd.DataFrame:
    """
    Читає реальні дані з таблиці round_history (нова таблиця в P1).
    Повертає DataFrame або порожній DataFrame якщо даних мало.
    """
    try:
        conn = psycopg2.connect(DB_URL)
        query = """
            SELECT
                rh.round_number,
                rh.team_count,
                rh.track,
                rh.actual_duration_minutes
            FROM round_history rh
            WHERE rh.actual_duration_minutes IS NOT NULL
              AND rh.actual_duration_minutes > 0
        """
        df = pd.read_sql(query, conn)
        conn.close()
        return df
    except Exception as e:
        print(f"[WARN] DB недоступна: {e}. Використовую синтетичні дані.")
        return pd.DataFrame()


def load_synthetic_data() -> pd.DataFrame:
    """Fallback: синтетичні дані якщо БД недоступна або даних мало."""
    if DATA_PATH.exists():
        with open(DATA_PATH) as f:
            return pd.DataFrame(json.load(f))

    # Вбудовані дані-мінімум (40 сесій × 5 раундів)
    records = []
    np.random.seed(42)
    BASE = {1: 12, 2: 14, 3: 16, 4: 17, 5: 15}
    TRACK_BONUS = {"A": 3, "B": 1, "C": 0}

    for _ in range(40):
        team_count = np.random.randint(3, 9)
        track = np.random.choice(["A", "A", "B", "B", "C"])
        for rn in range(1, 6):
            actual = (BASE[rn] + TRACK_BONUS[track]
                      + max(0, (team_count - 4)) * 0.4
                      + np.random.normal(0, 1.5))
            records.append({
                "round_number": rn,
                "team_count": team_count,
                "track": track,
                "actual_duration_minutes": float(np.clip(round(actual, 1), 8, 25))
            })
    return pd.DataFrame(records)


def build_pipeline() -> Pipeline:
    preprocessor = ColumnTransformer([
        ("num", "passthrough", ["round_number", "team_count"]),
        ("cat", OneHotEncoder(sparse_output=False, drop="first",
                              handle_unknown="ignore"), ["track"]),
    ])
    # alpha підбирається автоматично через крос-валідацію
    alphas = [0.001, 0.01, 0.1, 0.5, 1.0, 2.0, 5.0, 10.0, 50.0, 100.0]
    return Pipeline([
        ("prep", preprocessor),
        ("reg", RidgeCV(alphas=alphas, cv=5, scoring="neg_mean_absolute_error")),
    ])


def train_model(df: pd.DataFrame) -> tuple[Pipeline, int]:
    X = df[["round_number", "team_count", "track"]]
    y = df["actual_duration_minutes"]
    model = build_pipeline()
    model.fit(X, y)
    return model, len(df)


def confidence_score(n_samples: int) -> float:
    """
    Довіра до прогнозу залежить від кількості реальних спостережень.
    < 10  → низька (0.40–0.60)
    10–50 → середня (0.60–0.85)
    50+   → висока (0.85–0.95)
    """
    return float(np.clip(0.40 + n_samples * 0.011, 0.40, 0.95))


# ─── Завантаження/навчання моделі при старті ─────────────────────────────────
_model: Pipeline | None = None
_n_samples: int = 0
_trained_at: str = ""

@app.on_event("startup")
async def startup():
    global _model, _n_samples, _trained_at
    df = get_training_data_from_db()

    # Якщо реальних даних мало — доповнюємо синтетичними
    if len(df) < 20:
        print(f"[INFO] Реальних записів: {len(df)}. Доповнюємо синтетичними.")
        df_synth = load_synthetic_data()
        df = pd.concat([df, df_synth], ignore_index=True)

    _model, _n_samples = train_model(df)
    _trained_at = datetime.utcnow().isoformat()
    print(f"[INFO] Модель навчена на {_n_samples} записах. ({_trained_at})")


# ─── API ──────────────────────────────────────────────────────────────────────

class RecommendationResponse(BaseModel):
    recommended_minutes:  int
    confidence:           float
    n_training_samples:   int
    trained_at:           str
    note:                 str


@app.get("/recommend-duration", response_model=RecommendationResponse)
async def recommend_duration(
    teams: int   = Query(..., ge=1, le=20, description="Кількість команд"),
    track: str   = Query(..., description="Трек: A, B або C"),
    round: int   = Query(..., ge=1, le=5,  description="Номер раунду (1-5)"),
):
    """
    Рекомендує тривалість раунду в хвилинах на основі накопиченої статистики.

    Приклад: GET /recommend-duration?teams=6&track=A&round=3
    """
    if _model is None:
        raise HTTPException(503, "Модель ще не готова")

    track_upper = track.upper()
    if track_upper not in ["A", "B", "C"]:
        raise HTTPException(400, "track має бути A, B або C")

    X = pd.DataFrame([{
        "round_number": round,
        "team_count":   teams,
        "track":        track_upper,
    }])

    pred = float(_model.predict(X)[0])
    pred = max(8.0, min(30.0, pred))          # обмеження 8–30 хв
    conf = confidence_score(_n_samples)

    return RecommendationResponse(
        recommended_minutes=int(round(pred)),
        confidence=round(conf, 2),
        n_training_samples=_n_samples,
        trained_at=_trained_at,
        note="synthetic" if _n_samples <= 200 else "real_data",
    )


@app.post("/retrain")
async def retrain():
    """
    Перенавчає модель на свіжих даних з БД.
    Викликається з SessionsController після кожної завершеної сесії.
    """
    global _model, _n_samples, _trained_at
    df = get_training_data_from_db()
    if len(df) < 5:
        df_synth = load_synthetic_data()
        df = pd.concat([df, df_synth], ignore_index=True)

    _model, _n_samples = train_model(df)
    _trained_at = datetime.utcnow().isoformat()
    return {"status": "ok", "n_samples": _n_samples, "trained_at": _trained_at}


@app.get("/health")
async def health():
    return {
        "status": "ok",
        "model_ready": _model is not None,
        "n_samples": _n_samples,
        "trained_at": _trained_at,
    }
