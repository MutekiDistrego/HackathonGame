/** @type {import('tailwindcss').Config} */
export default {
  content: [
    "./index.html",
    "./src/**/*.{js,ts,jsx,tsx}",
  ],
  theme: {
    extend: {
      colors: {
        neon: {
          cyan: '#00f0ff',
          pink: '#ff00e5',
          green: '#39ff14',
          yellow: '#f0ff00',
          purple: '#bf00ff',
        },
        cyber: {
          dark: '#0a0a1a',
          darker: '#050510',
          card: '#111128',
          border: '#1a1a3e',
        },
      },
      fontFamily: {
        cyber: ['Orbitron', 'sans-serif'],
      },
      boxShadow: {
        neon: '0 0 10px rgba(0, 240, 255, 0.5), 0 0 40px rgba(0, 240, 255, 0.2)',
        'neon-pink': '0 0 10px rgba(255, 0, 229, 0.5), 0 0 40px rgba(255, 0, 229, 0.2)',
      },
    },
  },
  plugins: [],
}
