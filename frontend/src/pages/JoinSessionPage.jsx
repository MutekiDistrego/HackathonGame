import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import sessionApi from '../services/sessionApi'

function JoinSessionPage() {
  const navigate = useNavigate()
  const [step, setStep] = useState(1) // 1 = enter code, 2 = register team
  const [code, setCode] = useState('')
  const [sessionInfo, setSessionInfo] = useState(null)
  const [teamName, setTeamName] = useState('')
  const [members, setMembers] = useState([{ name: '', role: '' }])
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState('')

  const handleCheckSession = async (e) => {
    e.preventDefault()
    if (!code.trim()) return
    setLoading(true)
    setError('')
    try {
      const res = await sessionApi.getSession(code.toUpperCase())
      setSessionInfo(res.data)
      setCode(code.toUpperCase())
      setStep(2)
    } catch (err) {
      setError(err.response?.data?.message || 'Сесію не знайдено')
    } finally {
      setLoading(false)
    }
  }

  const addMember = () => {
    if (members.length < 6) {
      setMembers([...members, { name: '', role: '' }])
    }
  }

  const removeMember = (index) => {
    if (members.length > 1) {
      setMembers(members.filter((_, i) => i !== index))
    }
  }

  const updateMember = (index, field, value) => {
    setMembers(members.map((m, i) => i === index ? { ...m, [field]: value } : m))
  }

  const handleRegisterTeam = async (e) => {
    e.preventDefault()
    if (!teamName.trim()) {
      setError('Введіть назву команди')
      return
    }
    setLoading(true)
    setError('')
    try {
      const validMembers = members.filter(m => m.name.trim())
      const res = await sessionApi.registerTeam(code, {
        name: teamName,
        members: validMembers.length > 0 ? validMembers : undefined,
      })
      // Save session/team context for cross-project navigation
      localStorage.setItem('hackathon_session', code)
      localStorage.setItem('hackathon_teamId', String(res.data.id))
      localStorage.setItem('hackathon_teamName', res.data.name || teamName)
      navigate(`/session/${code}`)
    } catch (err) {
      setError(err.response?.data?.message || 'Помилка реєстрації команди')
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="min-h-screen flex items-center justify-center px-4">
      <div className="card-cyber w-full max-w-md">
        {step === 1 ? (
          <>
            <h1 className="font-cyber text-2xl text-neon-pink mb-8 text-center">
              Приєднатися до сесії
            </h1>
            <form onSubmit={handleCheckSession} className="flex flex-col gap-4">
              <input
                type="text"
                placeholder="Код кімнати (6 символів)"
                value={code}
                onChange={(e) => setCode(e.target.value.toUpperCase())}
                maxLength={6}
                className="input-cyber text-center text-2xl tracking-[0.5em] font-cyber"
                required
              />
              {error && <p className="text-red-400 text-sm text-center">{error}</p>}
              <button type="submit" disabled={loading} className="btn-neon-pink mt-4">
                {loading ? 'Перевірка...' : 'Далі'}
              </button>
            </form>
          </>
        ) : (
          <>
            <div className="flex justify-between items-center mb-6">
              <div>
                <h1 className="font-cyber text-xl text-neon-pink">Реєстрація команди</h1>
                <p className="text-gray-500 text-sm mt-1">
                  Сесія: <span className="text-neon-cyan">{sessionInfo?.name}</span>
                  <span className="text-gray-600 ml-2">({code})</span>
                </p>
              </div>
              <button
                onClick={() => { setStep(1); setError('') }}
                className="text-gray-500 hover:text-white text-sm"
              >
                Назад
              </button>
            </div>

            <form onSubmit={handleRegisterTeam} className="flex flex-col gap-4">
              <input
                type="text"
                placeholder="Назва команди *"
                value={teamName}
                onChange={(e) => setTeamName(e.target.value)}
                className="input-cyber"
                required
              />

              <div>
                <div className="flex justify-between items-center mb-2">
                  <p className="text-sm text-gray-400">Учасники</p>
                  <button
                    type="button"
                    onClick={addMember}
                    disabled={members.length >= 6}
                    className="text-xs text-neon-cyan hover:text-white disabled:opacity-40"
                  >
                    + Додати
                  </button>
                </div>
                <div className="space-y-2">
                  {members.map((m, i) => (
                    <div key={i} className="flex gap-2">
                      <input
                        type="text"
                        placeholder={`Учасник ${i + 1}`}
                        value={m.name}
                        onChange={(e) => updateMember(i, 'name', e.target.value)}
                        className="input-cyber text-sm flex-1"
                      />
                      <select
                        value={m.role}
                        onChange={(e) => updateMember(i, 'role', e.target.value)}
                        className="input-cyber text-sm w-32"
                      >
                        <option value="">Роль</option>
                        <option value="leader">Лідер</option>
                        <option value="developer">Розробник</option>
                        <option value="designer">Дизайнер</option>
                        <option value="presenter">Презентатор</option>
                        <option value="analyst">Аналітик</option>
                      </select>
                      {members.length > 1 && (
                        <button
                          type="button"
                          onClick={() => removeMember(i)}
                          className="text-red-400 hover:text-red-300 px-2"
                        >
                          &times;
                        </button>
                      )}
                    </div>
                  ))}
                </div>
              </div>

              {error && <p className="text-red-400 text-sm">{error}</p>}

              <button type="submit" disabled={loading} className="btn-neon-pink mt-2">
                {loading ? 'Реєстрація...' : 'Зареєструвати команду'}
              </button>
            </form>
          </>
        )}
      </div>
    </div>
  )
}

export default JoinSessionPage
