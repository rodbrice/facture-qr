import react from '@vitejs/plugin-react'
import { defineConfig } from 'vitest/config'

// The API that the development server forwards the API paths to (dotnet run, see README).
const api = 'http://127.0.0.1:5080'
const apiPaths = ['/customers', '/invoices', '/settings', '/health', '/openapi']

export default defineConfig({
  plugins: [react()],
  server: {
    proxy: Object.fromEntries(apiPaths.map((path) => [path, api])),
  },
  build: {
    // The production build is served by the API from its wwwroot folder.
    outDir: '../src/FactureQr.Api/wwwroot',
    emptyOutDir: true,
  },
  test: {
    environment: 'jsdom',
    setupFiles: ['./src/test-setup.ts'],
    include: ['src/**/*.test.{ts,tsx}'],
  },
})
