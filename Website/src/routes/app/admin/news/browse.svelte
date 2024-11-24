<script lang="ts">
  import type { Snippet } from 'svelte'
  import Overlay from '../../../overlay.svelte'
  import Button from '$lib/client/ui/button.svelte'
  import Icon from '$lib/client/ui/icon.svelte'
  import { writable, type Writable } from 'svelte/store'
  import FileSelector from '../../files/file-selector.svelte'
  import { FileAccessLevel, type FileResource, useServerContext } from '$lib/client/client'
  import { useDashboardContext, type DashboardContext } from '$lib/client/contexts/dashboard'

  const {
    ondismiss,
    browse,
    onresult,
    dashboard
  }: {
    onresult: (file: FileResource) => void
    ondismiss: () => void
    browse: Writable<[fileId: string | null] | null>
    dashboard: DashboardContext
  } = $props()
  const { getFileMime, getFileAccesses } = useServerContext()

  useDashboardContext(dashboard)
</script>

<div class="main">
  <p>
    Please select an image to be used as the news cover. If you can't see the files that you have
    uploaded, please make sure that it's publicly shared.
  </p>

  <FileSelector
    maxFileCount={1}
    onresult={async (files: FileResource[]) => {
      const fileMime = await getFileMime(files[0].id)

      onresult(files[0])

      $browse = null
    }}
    oncancel={() => {
      $browse = null
    }}
    mimeTypes={[new RegExp('^image/.*$')]}
    filter={async (file) => {
      const a = await getFileAccesses({
        targetFileId: file.file.id
      })

      const b = a.find((e) => e.targetUserId == null && e.level >= FileAccessLevel.Read) != null
      return b
    }}
  />
</div>

<style lang="scss">
  @use '../../../../global.scss' as *;

  div.main {
    @include force-size(calc(100dvw - 96px), calc(100dvh - 128px));
    flex-grow: 1;

    gap: 16px;
  }
</style>
