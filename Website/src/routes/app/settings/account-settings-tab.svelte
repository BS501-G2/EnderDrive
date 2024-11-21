<script lang="ts">
  import type { IconOptions } from '$lib/client/ui/icon.svelte'
  import type { Snippet } from 'svelte'
  import { useAccountSettingsContext } from './account-settings'

  const { name, icon, children }: { name: string; icon: IconOptions; children?: Snippet } = $props()
  const { pushTab } = useAccountSettingsContext()

  $effect(() => pushTab(name, icon, content))
</script>

{#snippet content()}
  {@render children?.()}
{/snippet}

<style lang="scss">
</style>
