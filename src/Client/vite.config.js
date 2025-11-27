import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// Adjust the proxy target if your API runs on a different port.
export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    proxy: {
      '/api': {
        target: 'http://localhost:5000',
        changeOrigin: true
      }
    }
  }
})
