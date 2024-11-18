<script lang="ts">
  import type { IconOptions } from '$lib/client/ui/icon.svelte'
  import { onMount, type Snippet } from 'svelte'

  const { pushTab }: { pushTab(name: string, icon: IconOptions, content: Snippet): void } = $props()

  onMount(() => pushTab('History', { icon: 'clock-rotate-left', thickness: 'solid' }, history))
</script>

{#snippet history()}{/snippet}
