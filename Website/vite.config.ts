import { sveltekit } from '@sveltejs/kit/vite';
import { defineConfig } from 'vite';

export default defineConfig({
	plugins: [sveltekit()],
	esbuild: {
		supported: {
			'top-level-await': true
		}
	},
	server: {
		proxy: {
			'/ws': {
				target: 'ws://localhost:8082',
				changeOrigin: true,
				secure: false,
				ws: true
			}
		}
	}
});
