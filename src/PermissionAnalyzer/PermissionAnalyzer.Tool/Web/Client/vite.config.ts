import { defineConfig } from 'vitest/config';
import react from '@vitejs/plugin-react';

export default defineConfig({
  plugins: [react()],
  // Stable embedded resource names; the CLI serves these without a filesystem/static-file root.
  build: {
    cssCodeSplit: false,
    rolldownOptions: {
      output: {
        entryFileNames: 'app.js',
        assetFileNames: 'styles[extname]',
      },
    },
  },
  server: {
    host: '127.0.0.1',
    port: 5173,
    strictPort: true,
    proxy: {
      '/api': {
        target: 'http://127.0.0.1:8765',
        changeOrigin: true,
        configure(proxy) {
          // The backend accepts only its own origin. This proxy is local development only.
          proxy.on('proxyReq', request => request.setHeader('Origin', 'http://127.0.0.1:8765'));
        },
      },
    },
  },
  test: { environment: 'jsdom', env: { NODE_ENV: 'test' }, setupFiles: ['./src/testing/setup.ts'] },
});
