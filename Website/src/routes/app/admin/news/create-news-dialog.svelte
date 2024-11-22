<script lang="ts">
  import { type Snippet } from 'svelte'
  import Overlay from '../../../overlay.svelte'
  import Button from '$lib/client/ui/button.svelte'
  import Icon from '$lib/client/ui/icon.svelte'
  import FileSelector from '../../files/file-selector.svelte'
  import { useServerContext, type FileResource } from '$lib/client/client'
  import { writable } from 'svelte/store'
  import { useDashboardContext } from '$lib/client/contexts/dashboard'
  import CreateDialog from './create-dialog.svelte'
  import Browse from './browse.svelte'
  import Window from '$lib/client/ui/window.svelte'

  const {
    newsId,
    oncancel,
    onresult
  }: {
    newsId?: number
    oncancel: () => void
    onresult: (file: FileResource) => void
  } = $props()

  const { getFileMime, createNews, getNews } = useServerContext()
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
