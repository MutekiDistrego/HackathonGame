import { Link } from 'react-router-dom'

function HomePage() {
  return (
    <div className="min-h-screen flex flex-col items-center justify-center px-4">
      <h1 className="font-cyber text-5xl md:text-7xl font-bold text-neon-cyan mb-4 text-center">
        AI HACKATHON
      </h1>
      <h2 className="font-cyber text-2xl md:text-3xl text-neon-pink mb-12">
        THE GAME
      </h2>

      <div className="flex flex-col gap-4 w-full max-w-sm">
        <Link to="/create" className="btn-neon text-center text-lg">
          Створити сесію
        </Link>
        <Link to="/join" className="btn-neon-pink text-center text-lg">
          Приєднатися
        </Link>
      </div>

      <p className="mt-12 text-gray-500 text-sm">
        Система управління ігровими сесіями
      </p>
    </div>
  )
}

export default HomePage
