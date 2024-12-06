<script lang="ts">
  import { type Writable } from 'svelte/store'
  import FileSelector from '../../files/file-selector.svelte'

  import { useDashboardContext, type DashboardContext } from '$lib/client/contexts/dashboard'
  import { FileAccessLevel, type FileResource } from '$lib/client/resource'
  import { useClientContext } from '$lib/client/client'
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
  // const { getFileMime, getFileAccesses } = useServerContext()
  const { server } = useClientContext()
  5
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
      const fileData = (
        await server.FileGetDataEntries({
          FileId: files[0].Id,
          Pagination: { Count: 1 }
        })
      )[0]

      const mime = await server.FileGetMime({
        FileId: files[0].Id,
        FileDataId: fileData.Id
      })

      if (!mime.startsWith('image/')) {
        throw new Error('Only image file type is allowed')
      }

      onresult(files[0])

      $browse = null
    }}
    oncancel={() => {
      $browse = null
    }}
    mimeTypes={[new RegExp('^image/.*$')]}
    filter={async (file) => {
      const fileAccesses = await server.GetFileAccesses({
        TargetFileId: file.file.Id,
        IncludePublic: true
      })

      return (
        fileAccesses.find((e) => e.TargetUserId == null && e.Level >= FileAccessLevel.Read) != null
      )
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
