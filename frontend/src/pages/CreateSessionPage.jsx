import { useState } from 'react'
import { useNavigate, Link } from 'react-router-dom'
import sessionApi from '../services/sessionApi'

function CreateSessionPage() {
  const navigate = useNavigate()
  const [name, setName] = useState('')
  const [adminPassword, setAdminPassword] = useState('')
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState('')

  const handleSubmit = async (e) => {
    e.preventDefault()
    setLoading(true)
    setError('')

    try {
      const response = await sessionApi.createSession({ name, adminPassword })
      navigate(`/session/${response.data.code}`)
    } catch (err) {
      setError(err.response?.data?.message || 'Помилка створення сесії')
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="min-h-screen flex items-center justify-center px-4">
      <div className="card-cyber w-full max-w-md">
        <div className="mb-6">
          <Link to="/" className="text-gray-400 hover:text-neon-cyan text-sm">← На головну</Link>
        </div>
        <h1 className="font-cyber text-2xl text-neon-cyan mb-8 text-center">
          Створити сесію
        </h1>

        <form onSubmit={handleSubmit} className="flex flex-col gap-4">
          <input
            type="text"
            placeholder="Назва сесії"
            value={name}
            onChange={(e) => setName(e.target.value)}
            className="input-cyber"
            required
          />
          <input
            type="password"
            placeholder="Пароль адміна"
            value={adminPassword}
            onChange={(e) => setAdminPassword(e.target.value)}
            className="input-cyber"
            required
          />

          {error && <p className="text-red-400 text-sm">{error}</p>}

          <button type="submit" disabled={loading} className="btn-neon mt-4">
            {loading ? 'Створення...' : 'Створити'}
          </button>
        </form>
      </div>
    </div>
  )
}

export default CreateSessionPage
