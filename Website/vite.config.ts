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
			'/socket.io': {
				target: 'ws://localhost:8083/socket.io',
				changeOrigin: false,
				secure: false,
				ws: true
			}
		}
	}
});
