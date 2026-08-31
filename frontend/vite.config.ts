import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

export default defineConfig({
  plugins: [react()],
  base: process.env.VITE_BASE_PATH ?? '/',
  server: {
    port: Number(process.env.VITE_DEV_PORT ?? 5173),
    proxy: {
      '/api': {
        target: `http://localhost:${process.env.VITE_API_PORT ?? 5001}`,
        changeOrigin: true,
      },
      '/gamehub': {
        target: `http://localhost:${process.env.VITE_API_PORT ?? 5001}`,
        changeOrigin: true,
        ws: true,
      },
    },
  },
})
