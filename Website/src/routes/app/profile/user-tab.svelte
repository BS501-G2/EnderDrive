<script lang="ts">
  import type { IconOptions } from '$lib/client/ui/icon.svelte'
  import { onMount, type Snippet } from 'svelte'
  import { useUserContext } from './user-context'

  const { children, label, icon }: { label: string; icon: IconOptions; children?: Snippet } =
    $props()

  const { pushTab } = useUserContext()

  onMount(() => pushTab(label, icon, content))
</script>

{#snippet content()}
  {@render children?.()}
{/snippet}
