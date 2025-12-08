import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
import * as path from 'node:path';
import { fileURLToPath } from 'node:url';

// @ts-ignore
const __dirname = path.dirname(fileURLToPath(new URL(import.meta.url)));

export default defineConfig({
    plugins: [react()],
    resolve: {
        alias: {
            '@core': path.resolve(__dirname, './src/core'),
            '@components': path.resolve(__dirname, './src/components'),
            '@lib': path.resolve(__dirname, './src/lib'),
        },
    },
    server: {
        open: '/login', 
    },
})
