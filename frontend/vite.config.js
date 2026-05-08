import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

const backendUrl = process.env.BACKEND_URL || 'http://localhost:8081'

export default defineConfig({
  plugins: [react()],
  server: {
    port: 3001,
    proxy: {
      '/api': {
        target: backendUrl,
        changeOrigin: true,
      },
      '/hubs': {
        target: backendUrl,
        changeOrigin: true,
        ws: true,
      },
    },
  },
})
