<script lang="ts">
  import { type Snippet } from 'svelte'
  import { writable } from 'svelte/store'
  import { useDashboardContext } from '$lib/client/contexts/dashboard'
  import CreateDialog from './create-dialog.svelte'
  import Browse from './browse.svelte'
  import Window from '$lib/client/ui/window.svelte'
  import type { FileResource } from '$lib/client/resource'

  const {
    newsId,
    oncancel,
    onresult
  }: {
    newsId?: number
    oncancel: () => void
    onresult: (file: FileResource) => void
  } = $props()

  const dashboardContext = useDashboardContext()
  const browse = writable<[fileId: string | null] | null>(null)
</script>

{#if $browse != null}
  <Window
    ondismiss={() => {
      $browse = null
    }}
    title="File Selection"
  >
    <Browse {browse} {onresult} ondismiss={oncancel} dashboard={dashboardContext} />
  </Window>
{:else}
  <CreateDialog ondismiss={oncancel} {browse} />
{/if}
