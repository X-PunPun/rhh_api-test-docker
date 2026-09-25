import react from '@vitejs/plugin-react'
import { defineConfig } from 'vite'

// En desarrollo, /api y /health se reenvían a la API .NET (evita CORS y deja el token fuera de la URL).
const api = process.env.RRHH_API_URL ?? 'https://localhost:7052'

export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    proxy: {
      '/api': { target: api, changeOrigin: true, secure: false },
      '/health': { target: api, changeOrigin: true, secure: false },
    },
  },
})
