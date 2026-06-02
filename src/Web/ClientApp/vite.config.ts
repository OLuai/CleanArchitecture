import path from 'node:path';
import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
import tailwindcss from '@tailwindcss/vite';
import { tanstackRouter } from '@tanstack/router-plugin/vite';

const target =
  process.env['services__webapi__https__0'] ||
  process.env['services__webapi__http__0'];

const proxyOptions = target
  ? { target, secure: false, changeOrigin: true }
  : undefined;

// https://vitejs.dev/config/
export default defineConfig({
  plugins: [
    tanstackRouter({ target: 'react', autoCodeSplitting: true }),
    react(),
    tailwindcss(),
  ],
  resolve: {
    alias: {
      '@': path.resolve(__dirname, './src'),
    },
  },
  server: {
    port: parseInt(process.env.PORT ?? '5173'),
    proxy: proxyOptions
      ? {
          '/api': proxyOptions,
          '/openapi': proxyOptions,
          '/scalar': proxyOptions,
          '/weatherforecast': proxyOptions,
          '/WeatherForecast': proxyOptions,
        }
      : undefined,
  },
  build: {
    outDir: 'build',
  },
});
