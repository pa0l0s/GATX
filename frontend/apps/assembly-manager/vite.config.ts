import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
import { dirname, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

const appRoot = dirname(fileURLToPath(import.meta.url));

export default defineConfig({
  root: appRoot,
  plugins: [react()],
  server: {
    port: 4200,
    proxy: {
      '/api': 'http://localhost:5080'
    }
  },
  build: {
    outDir: resolve(appRoot, 'dist')
  }
});
