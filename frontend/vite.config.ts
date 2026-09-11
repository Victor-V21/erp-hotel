import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import tailwindcss from '@tailwindcss/vite'
import path from 'node:path'

export default defineConfig({
  plugins: [react(), tailwindcss()],
  build: {
    outDir: path.resolve(import.meta.dirname, '../backend/src/hotel-erp.Api/wwwroot'),
    emptyOutDir: true,
  },
  resolve: {
    alias: {
      '@': path.resolve(import.meta.dirname, './src'),
    },
  },
  server: {
    port: 5173,
    proxy: {
      '/api': {
        target: 'http://localhost:5084',
        changeOrigin: true,
      },
      '/hubs': {
        target: 'http://localhost:5084',
        ws: true,
      },
    },
  },
})
