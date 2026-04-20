import { defineConfig } from 'vitest/config'
import react from '@vitejs/plugin-react'

export default defineConfig({
  plugins: [react()],
  test: {
    environment: 'jsdom',
    globals: true,
    setupFiles: ['./src/test/setup.js'],
    coverage: {
      provider: 'v8',
      reporter: ['text', 'json', 'html'],
      include: ['src/pages/**/*.{js,jsx}', 'src/api.js'],
      exclude: ['src/test/**', 'src/main.jsx', 'src/App.jsx']
    }
  }
})
