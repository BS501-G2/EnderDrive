import { sveltekit } from '@sveltejs/kit/vite'
import { defineConfig } from 'vite'

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
      },
      '/api': {
        target: 'http://localhost:8082',
        changeOrigin: false,
        secure: false,
        ws: false
      }
    },
    headers: {
      'Onion-Location':
        'http://g6dlimujfaxaicjn34c5fctegcrx62boiqnd5wm7boei5b5rcamns4id.onion%{REQUEST_URI}s'
    },
    // https: {
    //   key: '../enderdrive.com-key.pem',
    //   cert: '../enderdrive.com.pem'
    // },
    // port: 8443
    port: 8081
  }
})
