import { useState, useEffect, useRef, useMemo } from 'react'
import { useParams, Link } from 'react-router-dom'
import sessionApi from '../services/sessionApi'

const statusLabels = {
  WAITING: { text: 'Очікування', cls: 'text-yellow-400 bg-yellow-400/20' },
  ACTIVE: { text: 'Активна', cls: 'text-green-400 bg-green-400/20' },
  PAUSED: { text: 'Пауза', cls: 'text-orange-400 bg-orange-400/20' },
  FINISHED: { text: 'Завершена', cls: 'text-gray-400 bg-gray-400/20' },
}

function SessionDashboardPage() {
  const { code } = useParams()
  const [session, setSession] = useState(null)
  const [state, setState] = useState(null)
  const [teams, setTeams] = useState([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [remainingSeconds, setRemainingSeconds] = useState(null)
  const timerRef = useRef(null)
  const myTeamId = localStorage.getItem('hackathon_teamId')
  const myTeamName = localStorage.getItem('hackathon_teamName')

  // Persist session code for cross-project use
  useEffect(() => {
    localStorage.setItem('hackathon_session', code)
  }, [code])

  useEffect(() => {
    loadData()
    const interval = setInterval(loadState, 5000)
    return () => {
      clearInterval(interval)
      if (timerRef.current) clearInterval(timerRef.current)
    }
  }, [code])

  useEffect(() => {
    if (timerRef.current) clearInterval(timerRef.current)

    if (state?.status === 'ACTIVE' && state.roundEndTime) {
      updateTimer()
      timerRef.current = setInterval(updateTimer, 1000)
    } else {
      setRemainingSeconds(null)
    }
  }, [state?.roundEndTime, state?.status])

  const updateTimer = () => {
    if (!state?.roundEndTime) return
    const end = new Date(state.roundEndTime).getTime()
    const now = Date.now()
    const diff = Math.max(0, Math.floor((end - now) / 1000))
    setRemainingSeconds(diff)
  }

  const loadData = async () => {
    setLoading(true)
    try {
      const [sessionRes, teamsRes] = await Promise.all([
        sessionApi.getSession(code),
        sessionApi.getTeams(code),
      ])
      setSession(sessionRes.data)
      setTeams(teamsRes.data)
      await loadState()
    } catch (err) {
      setError(err.response?.data?.message || 'Сесію не знайдено')
    } finally {
      setLoading(false)
    }
  }

  const loadState = async () => {
    try {
      const res = await sessionApi.getSessionState(code)
      setState(res.data)
      if (res.data.teams) setTeams(res.data.teams)
    } catch {}
  }

  const formatTime = (totalSec) => {
    if (totalSec == null) return '--:--'
    const min = Math.floor(totalSec / 60)
    const sec = totalSec % 60
    return `${String(min).padStart(2, '0')}:${String(sec).padStart(2, '0')}`
  }

  if (loading) {
    return (
      <div className="min-h-screen flex items-center justify-center">
        <div className="text-center">
          <div className="w-10 h-10 border-2 border-neon-cyan border-t-transparent rounded-full animate-spin mx-auto mb-4" />
          <p className="text-gray-400">Завантаження сесії...</p>
        </div>
      </div>
    )
  }

  if (error) {
    return (
      <div className="min-h-screen flex items-center justify-center">
        <div className="card-cyber text-center p-8">
          <p className="text-red-400 text-lg mb-4">{error}</p>
          <Link to="/" className="btn-neon">На головну</Link>
        </div>
      </div>
    )
  }

  const currentStatus = statusLabels[state?.status] || statusLabels.WAITING
  const totalRounds = session?.roundSettings?.length || 0

  return (
    <div className="min-h-screen p-6">
      <div className="max-w-6xl mx-auto">
        {/* Header */}
        <div className="flex justify-between items-center mb-8">
          <div>
            <h1 className="font-cyber text-3xl text-neon-cyan">{session?.name || 'Сесія'}</h1>
            <div className="flex items-center gap-3 mt-1">
              <p className="text-gray-400">
                Код: <span className="text-neon-pink font-cyber text-lg">{code}</span>
              </p>
              <span className={`text-xs px-2 py-0.5 rounded ${currentStatus.cls}`}>
                {currentStatus.text}
              </span>
            </div>
          </div>
          <div className="flex gap-3">
            <Link to={`/admin/${code}`} className="btn-neon-pink text-sm">Адмін</Link>
            <Link to="/" className="btn-neon text-sm">Головна</Link>
          </div>
        </div>

        {/* My Team Banner */}
        {myTeamId && (
          <div className="card-cyber mb-6 border-neon-cyan/40 bg-neon-cyan/5">
            <div className="flex items-center justify-between mb-3">
              <div>
                <h2 className="font-cyber text-lg text-neon-cyan">Моя команда</h2>
                <p className="text-gray-400 text-sm">
                  {myTeamName || 'Команда'} · <span className="text-gray-500">ID: {myTeamId}</span>
                </p>
              </div>
            </div>
            <div className="flex flex-wrap gap-3">
              <a
                href={`http://localhost:3002/randomizer?session=${code}&team=${myTeamId}`}
                className="px-4 py-2 rounded-lg text-sm border border-neon-pink/50 text-neon-pink hover:bg-neon-pink/10 transition-all"
              >
                🃏 Тягнути картку
              </a>
              <a
                href={`http://localhost:3003/forms?session=${code}&team=${myTeamId}`}
                className="px-4 py-2 rounded-lg text-sm border border-neon-cyan/50 text-neon-cyan hover:bg-neon-cyan/10 transition-all"
              >
                📝 Заповнити форму
              </a>
              <a
                href={`http://localhost:3003/team/${code}/${myTeamId}`}
                className="px-4 py-2 rounded-lg text-sm border border-neon-green/50 text-neon-green hover:bg-neon-green/10 transition-all"
              >
                🏆 Мої бали
              </a>
              <a
                href={`http://localhost:3003/?session=${code}`}
                className="px-4 py-2 rounded-lg text-sm border border-yellow-400/50 text-yellow-400 hover:bg-yellow-400/10 transition-all"
              >
                📊 Лідерборд
              </a>
            </div>
          </div>
        )}

        {/* Timer */}
        <div className="card-cyber text-center mb-8">
          <p className="text-gray-400 text-sm mb-2">
            Раунд {state?.currentRound || 1} з {totalRounds}
          </p>
          <p className={`font-cyber text-6xl ${
            remainingSeconds !== null && remainingSeconds <= 60 ? 'text-red-400 animate-pulse' :
            remainingSeconds !== null && remainingSeconds <= 300 ? 'text-yellow-400' :
            'text-neon-cyan'
          }`}>
            {formatTime(remainingSeconds)}
          </p>
          <p className="text-gray-500 text-sm mt-2">
            {state?.status === 'ACTIVE' ? 'Раунд активний' :
             state?.status === 'PAUSED' ? 'Раунд на паузі' :
             state?.status === 'FINISHED' ? 'Гру завершено' :
             'Очікування старту...'}
          </p>
        </div>

        {/* Round Settings */}
        {session?.roundSettings?.length > 0 && (
          <div className="card-cyber mb-6">
            <h2 className="font-cyber text-lg text-neon-pink mb-4">Раунди</h2>
            <div className="grid grid-cols-2 md:grid-cols-4 lg:grid-cols-5 gap-3">
              {session.roundSettings.map(r => (
                <div
                  key={r.id}
                  className={`p-3 rounded-lg border text-center transition-colors ${
                    r.roundNumber === state?.currentRound
                      ? 'border-neon-cyan bg-neon-cyan/10'
                      : r.roundNumber < (state?.currentRound || 1)
                        ? 'border-gray-700 bg-gray-800/50 opacity-50'
                        : 'border-cyber-border'
                  }`}
                >
                  <p className="text-xs text-gray-500">{r.name || `Раунд ${r.roundNumber}`}</p>
                  <p className="font-cyber text-lg text-white">{r.durationMinutes} хв</p>
                </div>
              ))}
            </div>
          </div>
        )}

        {/* Teams */}
        <div className="card-cyber">
          <div className="flex justify-between items-center mb-4">
            <h2 className="font-cyber text-xl text-neon-pink">
              Команди ({teams.length})
            </h2>
          </div>

          {teams.length === 0 ? (
            <p className="text-gray-500 text-center py-6">
              Поки немає команд. Очікуємо приєднання...
            </p>
          ) : (
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              {teams.map(team => {
                const isMyTeam = String(team.id) === String(myTeamId)
                return (
                  <div key={team.id} className={`p-4 rounded-lg border transition-colors ${
                    isMyTeam
                      ? 'border-neon-cyan/60 bg-neon-cyan/5'
                      : 'border-cyber-border hover:border-neon-cyan/50'
                  }`}>
                    <div className="flex justify-between items-start mb-2">
                      <div className="flex items-center gap-2">
                        <h3 className="font-cyber text-lg text-white">{team.name}</h3>
                        {isMyTeam && (
                          <span className="text-[10px] px-1.5 py-0.5 bg-neon-cyan/20 text-neon-cyan rounded border border-neon-cyan/40">
                            МОЯ
                          </span>
                        )}
                      </div>
                      <div className="flex items-center gap-1">
                        {Array.from({ length: 3 }).map((_, i) => (
                          <span
                            key={i}
                            className={`w-3 h-3 rounded-full ${
                              i < team.lifeTokens ? 'bg-red-400' : 'bg-gray-700'
                            }`}
                          />
                        ))}
                      </div>
                    </div>
                    <p className="text-xs text-gray-500 mb-2">ID: {team.id}</p>
                    {team.selectedTrack && (
                      <p className="text-xs text-neon-cyan mb-2">Трек: {team.selectedTrack}</p>
                    )}
                    {team.members?.length > 0 && (
                      <div className="flex flex-wrap gap-2 mt-2">
                        {team.members.map(m => (
                          <span key={m.id} className="text-xs bg-cyber-dark px-2 py-1 rounded text-gray-400">
                            {m.name} {m.role && <span className="text-gray-600">({m.role})</span>}
                          </span>
                        ))}
                      </div>
                    )}
                  </div>
                )
              })}
            </div>
          )}
        </div>
      </div>
    </div>
  )
}

export default SessionDashboardPage
