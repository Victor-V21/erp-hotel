import base from '../../../frontend/vite.config.ts'
import { fileURLToPath } from 'node:url'
const here = fileURLToPath(new URL('.', import.meta.url))
export default {
  ...base,
  root: fileURLToPath(new URL('../../../frontend', import.meta.url)),
  cacheDir: here + 'vite-cache',
  server: { host: '127.0.0.1', port: 5179, strictPort: true,
    proxy: { '/api': { target: 'http://127.0.0.1:5089', changeOrigin: true } } },
  build: { outDir: here + 'frontend-build', emptyOutDir: false },
}
