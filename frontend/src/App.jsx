import { Routes, Route, Link, useLocation } from 'react-router-dom'
import HomePage from './pages/HomePage'
import CreateSessionPage from './pages/CreateSessionPage'
import JoinSessionPage from './pages/JoinSessionPage'
import SessionDashboardPage from './pages/SessionDashboardPage'
import AdminPanelPage from './pages/AdminPanelPage'

const PROJECTS = [
  { label: '🎮 Сесії', href: 'http://localhost:3001', port: 3001 },
  { label: '🃏 Картки', href: 'http://localhost:3002', port: 3002 },
  { label: '🏆 Бали & Форми', href: 'http://localhost:3003', port: 3003 },
]

function App() {
  const location = useLocation()
  const currentPort = window.location.port ? parseInt(window.location.port) : 3001

  return (
    <div className="min-h-screen bg-cyber-darker">
      {/* Cross-Project Navigation Bar */}
      <div className="bg-cyber-darker border-b border-cyber-border/50">
        <div className="max-w-7xl mx-auto px-4 flex items-center justify-between h-10">
          <span className="font-cyber text-xs text-gray-500 tracking-widest">AI HACKATHON: THE GAME</span>
          <div className="flex gap-1">
            {PROJECTS.map(p => (
              <a
                key={p.port}
                href={p.href}
                className={`px-3 py-1 rounded text-xs transition-all ${
                  currentPort === p.port
                    ? 'bg-neon-cyan/20 text-neon-cyan border border-neon-cyan/40'
                    : 'text-gray-500 hover:text-white hover:bg-white/5'
                }`}
              >
                {p.label}
              </a>
            ))}
          </div>
        </div>
      </div>

      <Routes>
        <Route path="/" element={<HomePage />} />
        <Route path="/create" element={<CreateSessionPage />} />
        <Route path="/join" element={<JoinSessionPage />} />
        <Route path="/session/:code" element={<SessionDashboardPage />} />
        <Route path="/admin/:code" element={<AdminPanelPage />} />
      </Routes>
    </div>
  )
}

export default App
