import { useState, useEffect } from 'react'
import { useParams, Link } from 'react-router-dom'
import sessionApi from '../services/sessionApi'

function AdminPanelPage() {
  const { code } = useParams()
  const [session, setSession] = useState(null)
  const [state, setState] = useState(null)
  const [teams, setTeams] = useState([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [actionMsg, setActionMsg] = useState('')

  useEffect(() => {
    loadData()
    const interval = setInterval(loadState, 5000)
    return () => clearInterval(interval)
  }, [code])

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

  const showMsg = (msg) => {
    setActionMsg(msg)
    setTimeout(() => setActionMsg(''), 3000)
  }

  const handleStartRound = async () => {
    try {
      const res = await sessionApi.startRound(code)
      showMsg(`Раунд ${res.data.round} розпочато! (${res.data.durationMinutes} хв)`)
      await loadState()
    } catch (err) {
      showMsg('Помилка: ' + (err.response?.data?.message || 'не вдалося'))
    }
  }

  const handlePauseRound = async () => {
    try {
      await sessionApi.pauseRound(code)
      showMsg('Раунд на паузі')
      await loadState()
    } catch {
      showMsg('Помилка при паузі')
    }
  }

  const handleNextRound = async () => {
    try {
      const res = await sessionApi.nextRound(code)
      showMsg(`Перехід до раунду ${res.data.currentRound}`)
      await loadState()
    } catch {
      showMsg('Помилка при переході')
    }
  }

  const handleAdjustTime = async (minutes) => {
    try {
      await sessionApi.adjustTime(code, minutes)
      showMsg(`Час ${minutes > 0 ? '+' : ''}${minutes} хв`)
      await loadState()
    } catch {
      showMsg('Помилка зміни часу')
    }
  }

  const handleStatusChange = async (status) => {
    try {
      await sessionApi.updateSessionStatus(code, status)
      showMsg(`Статус: ${status}`)
      await loadState()
    } catch {
      showMsg('Помилка зміни статусу')
    }
  }

  if (loading) {
    return (
      <div className="min-h-screen flex items-center justify-center">
        <div className="w-10 h-10 border-2 border-neon-cyan border-t-transparent rounded-full animate-spin" />
      </div>
    )
  }

  if (error) {
    return (
      <div className="min-h-screen flex items-center justify-center">
        <div className="card-cyber text-center p-8">
          <p className="text-red-400 mb-4">{error}</p>
          <Link to="/" className="btn-neon">На головну</Link>
        </div>
      </div>
    )
  }

  return (
    <div className="min-h-screen p-6">
      <div className="max-w-4xl mx-auto">
        {/* Header */}
        <div className="flex justify-between items-center mb-2">
          <h1 className="font-cyber text-3xl text-neon-cyan">Адмін панель</h1>
          <div className="flex gap-3">
            <a href="http://localhost:3002/admin" className="text-xs px-3 py-1.5 rounded border border-gray-600 text-gray-400 hover:text-white hover:border-gray-400 transition-all">
              Адмін карток →
            </a>
            <a href="http://localhost:3003/admin" className="text-xs px-3 py-1.5 rounded border border-gray-600 text-gray-400 hover:text-white hover:border-gray-400 transition-all">
              Адмін балів →
            </a>
            <Link to={`/session/${code}`} className="btn-neon text-sm">Дашборд</Link>
            <Link to="/" className="btn-neon text-sm">Головна</Link>
          </div>
        </div>
        <div className="flex items-center gap-3 mb-8">
          <p className="text-gray-400">
            Сесія: <span className="text-neon-pink font-cyber">{code}</span>
          </p>
          <span className="text-xs text-gray-500">
            Раунд {state?.currentRound || 1} | Статус: {state?.status || '—'}
          </span>
        </div>

        {/* Action Message */}
        {actionMsg && (
          <div className="mb-4 p-3 rounded-lg border border-neon-cyan bg-neon-cyan/10 text-neon-cyan text-sm">
            {actionMsg}
          </div>
        )}

        {/* Round Controls */}
        <div className="card-cyber mb-6">
          <h2 className="font-cyber text-lg text-neon-pink mb-4">Управління раундами</h2>
          <div className="flex gap-3 flex-wrap">
            <button
              onClick={handleStartRound}
              disabled={state?.status === 'ACTIVE'}
              className="btn-neon disabled:opacity-40 disabled:cursor-not-allowed"
            >
              Старт раунду
            </button>
            <button
              onClick={handlePauseRound}
              disabled={state?.status !== 'ACTIVE'}
              className="btn-neon-pink disabled:opacity-40 disabled:cursor-not-allowed"
            >
              Пауза
            </button>
            <button onClick={handleNextRound} className="btn-neon">
              Наступний раунд
            </button>
          </div>
        </div>

        {/* Timer Controls */}
        <div className="card-cyber mb-6">
          <h2 className="font-cyber text-lg text-neon-pink mb-4">Таймер</h2>
          <div className="flex gap-3 flex-wrap">
            <button onClick={() => handleAdjustTime(1)} className="btn-neon text-sm">+1 хв</button>
            <button onClick={() => handleAdjustTime(5)} className="btn-neon text-sm">+5 хв</button>
            <button onClick={() => handleAdjustTime(10)} className="btn-neon text-sm">+10 хв</button>
            <button onClick={() => handleAdjustTime(-1)} className="btn-neon-pink text-sm">-1 хв</button>
            <button onClick={() => handleAdjustTime(-5)} className="btn-neon-pink text-sm">-5 хв</button>
          </div>
        </div>

        {/* Session Status */}
        <div className="card-cyber mb-6">
          <h2 className="font-cyber text-lg text-neon-pink mb-4">Статус сесії</h2>
          <div className="flex gap-3 flex-wrap">
            {['WAITING', 'ACTIVE', 'PAUSED', 'FINISHED'].map(s => (
              <button
                key={s}
                onClick={() => handleStatusChange(s)}
                className={`px-4 py-2 rounded-lg text-sm font-medium transition-all border ${
                  state?.status === s
                    ? 'border-neon-cyan bg-neon-cyan/20 text-neon-cyan'
                    : 'border-cyber-border text-gray-400 hover:border-gray-500'
                }`}
              >
                {s}
              </button>
            ))}
          </div>
        </div>

        {/* Teams Management */}
        <div className="card-cyber mb-6">
          <h2 className="font-cyber text-lg text-neon-pink mb-4">
            Команди ({teams.length})
          </h2>

          {teams.length === 0 ? (
            <p className="text-gray-500">Команд ще немає</p>
          ) : (
            <div className="space-y-3">
              {teams.map(team => (
                <div key={team.id} className="p-4 rounded-lg border border-cyber-border">
                  <div className="flex justify-between items-start">
                    <div>
                      <h3 className="text-white font-medium">{team.name}</h3>
                      <div className="flex items-center gap-3 mt-1">
                        <span className="text-xs text-gray-500">ID: {team.id}</span>
                        <span className="text-xs text-gray-500">
                          {team.members?.length || 0} учасників
                        </span>
                      </div>
                    </div>
                    <button
                      onClick={async () => {
                        if (!confirm(`Видалити команду "${team.name}"?`)) return
                        try {
                          await sessionApi.deleteTeam(code, team.id)
                          showMsg(`Команду "${team.name}" видалено`)
                          await loadData()
                        } catch { showMsg('Помилка видалення') }
                      }}
                      className="text-red-400 hover:text-red-300 text-xs"
                    >
                      Видалити
                    </button>
                  </div>

                  {/* Life Tokens */}
                  <div className="flex items-center gap-3 mt-3">
                    <span className="text-sm text-gray-400">Життя:</span>
                    {[0, 1, 2].map(i => (
                      <button
                        key={i}
                        onClick={async () => {
                          const newTokens = i < team.lifeTokens ? i : i + 1
                          try {
                            await sessionApi.updateTokens(code, team.id, newTokens)
                            showMsg(`${team.name}: ${newTokens} токенів`)
                            await loadState()
                          } catch { showMsg('Помилка зміни токенів') }
                        }}
                        className={`w-5 h-5 rounded-full cursor-pointer transition-colors ${
                          i < team.lifeTokens ? 'bg-red-400 hover:bg-red-500' : 'bg-gray-700 hover:bg-gray-600'
                        }`}
                      />
                    ))}
                    <span className="text-xs text-gray-600">{team.lifeTokens}/3</span>
                  </div>

                  {/* Track Selection */}
                  <div className="flex items-center gap-3 mt-3">
                    <span className="text-sm text-gray-400">Трек:</span>
                    {['A', 'B', 'C'].map(track => (
                      <button
                        key={track}
                        onClick={async () => {
                          try {
                            await sessionApi.selectTrack(code, team.id, track)
                            showMsg(`${team.name}: Трек ${track}`)
                            await loadState()
                          } catch { showMsg('Помилка вибору треку') }
                        }}
                        className={`w-8 h-8 rounded text-xs font-bold transition-all ${
                          team.selectedTrack === track
                            ? 'bg-neon-cyan text-cyber-darker'
                            : 'border border-cyber-border text-gray-500 hover:border-neon-cyan hover:text-neon-cyan'
                        }`}
                      >
                        {track}
                      </button>
                    ))}
                  </div>

                  {/* Members */}
                  {team.members?.length > 0 && (
                    <div className="flex flex-wrap gap-2 mt-3">
                      {team.members.map(m => (
                        <span key={m.id} className="text-xs bg-cyber-dark px-2 py-1 rounded text-gray-400">
                          {m.name} {m.role && `(${m.role})`}
                        </span>
                      ))}
                    </div>
                  )}
                </div>
              ))}
            </div>
          )}
        </div>

        {/* Round Settings */}
        {session?.roundSettings?.length > 0 && (
          <div className="card-cyber">
            <h2 className="font-cyber text-lg text-neon-pink mb-4">Налаштування раундів</h2>
            <div className="space-y-2">
              {session.roundSettings.map(r => (
                <div
                  key={r.id}
                  className={`flex justify-between items-center p-3 rounded-lg border ${
                    r.roundNumber === state?.currentRound
                      ? 'border-neon-cyan bg-neon-cyan/10'
                      : 'border-cyber-border'
                  }`}
                >
                  <span className="text-white text-sm">{r.name || `Раунд ${r.roundNumber}`}</span>
                  <span className="text-gray-400 text-sm">{r.durationMinutes} хв</span>
                </div>
              ))}
            </div>
          </div>
        )}
      </div>
    </div>
  )
}

export default AdminPanelPage
