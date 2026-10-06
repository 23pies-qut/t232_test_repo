// Vite dev server (port 5188). Requests starting with /api are proxied to MockModule.Server (http://localhost:5098),
// so the browser talks to a single origin and no CORS is needed. The ports must match MockModule.Server's csproj / launchSettings.
import { defineConfig } from 'vite';
import plugin from '@vitejs/plugin-react';

export default defineConfig({
  plugins: [plugin()],
  server: {
    port: 5188,
    strictPort: true,
    proxy: { '^/api': 'http://localhost:5098' }
  }
});
