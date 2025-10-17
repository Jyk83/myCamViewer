import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  server: {
    host: '0.0.0.0',
    port: 5173,
    strictPort: true,
    allowedHosts: [
      '.sandbox.novita.ai',
      '5173-i07elqkb0lwzw1dgus9v7-de59bda9.sandbox.novita.ai',
    ],
    hmr: {
      clientPort: 5173,
    },
  },
  preview: {
    host: '0.0.0.0',
    port: 5173,
    strictPort: true,
  },
})
