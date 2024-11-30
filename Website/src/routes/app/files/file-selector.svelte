<script lang="ts">
  import {
    createFileBrowserContext,
    FileBrowserResolveType,
    type FileBrowserOptions,
    type FileBrowserResolve,
    type FileEntry
  } from '$lib/client/contexts/file-browser'
  import { derived, writable, type Writable } from 'svelte/store'
  import FileSelectorInner from './file-selector-inner.svelte'
  import { useClientContext } from '$lib/client/client'
  import { FileType, type FileResource } from '$lib/client/resource'

  const {
    maxFileCount,
    onresult,
    mimeTypes,
    oncancel,
    filter
  }: {
    maxFileCount: number
    onresult: (files: FileResource[]) => Promise<void>
    mimeTypes: (string | RegExp)[]
    filter: (file: FileEntry) => boolean | Promise<boolean>
    oncancel: () => void
  } = $props()

  const { server } = useClientContext()

  const resolve: Writable<Exclude<FileBrowserResolve, [FileBrowserResolveType.Trash]>> = writable([
    FileBrowserResolveType.File,
    null
  ])

  const selectMode: FileBrowserOptions['selectMode'] = {
    maxSelectionCount: maxFileCount,
    allowedFileMimeTypes: mimeTypes,
    filter
  }

  async function onfiles(
    event: MouseEvent & {
      currentTarget: EventTarget & HTMLButtonElement
    },
    fileIds: string[]
  ) {
    if (fileIds.length === 0) {
      return
    }

    const files = await Promise.all(
      fileIds.map(async (fileId) => {
        const file = await server.GetFile({ FileId: fileId })

        return file
      })
    )

    if (files.length === 1) {
      if (files[0].Type === FileType.Folder) {
        resolve.set([FileBrowserResolveType.File, files[0].Id])
        return
      }

      onresult([files[0]])
    } else {
      const filtered = files.filter((file) => file.Type === FileType.Folder)

      onresult(filtered)
    }
  }
  const fileBrowserContext = createFileBrowserContext(
    (event, fileId) => onfiles(event, fileId != null ? [fileId] : []),
    selectMode
  )

  const selectedFiles = derived(fileBrowserContext.fileListContext, (files) => {
    return files?.selectedFileIds ?? writable([])
  })
</script>

<FileSelectorInner
  {resolve}
  {selectMode}
  selectedFiles={$selectedFiles}
  {oncancel}
  {fileBrowserContext}
  {maxFileCount}
  onfiles={(event, files) => onfiles(event, files)}
/>
