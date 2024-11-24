<script lang="ts">
  import type { Writable } from 'svelte/store'

  const {
    pwaAvailable
  }: { pwaAvailable: Writable<{ pwa: boolean; install: () => Promise<void> } | null> } = $props()
</script>

<svelte:window
  onbeforeinstallprompt={(event) => {
    event.preventDefault()
    $pwaAvailable = {
      pwa: true,
      install: async () => {
        $pwaAvailable = null
        ;(event as any).prompt()

        const { outcome } = await (event as any).userChoice

        if (outcome === 'accepted') {
          $pwaAvailable = null
        } else {
        }
      }
    }
  }}
/>
