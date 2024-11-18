<script lang="ts">
  import {
    createFileBrowserContext,
    FileBrowserResolveType,
    type FileBrowserOptions,
    type FileBrowserResolve
  } from '$lib/client/contexts/file-browser'
  import { derived, writable, type Writable } from 'svelte/store'
  import FileSelectorInner from './file-selector-inner.svelte'
  import { FileType, useServerContext, type FileResource } from '$lib/client/client'

  const {
    maxFileCount,
    onresult,
    mimeTypes,
    oncancel
  }: {
    maxFileCount: number
    onresult: (files: FileResource[]) => Promise<void>
    mimeTypes: (string | RegExp)[]
    oncancel: () => void
  } = $props()

  const { getFile } = useServerContext()

  const resolve: Writable<Exclude<FileBrowserResolve, [FileBrowserResolveType.Trash]>> = writable([
    FileBrowserResolveType.File,
    null
  ])

  const selectMode: FileBrowserOptions['selectMode'] = {
    maxSelectionCount: maxFileCount,
    allowedFileMimeTypes: mimeTypes
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
        const file = await getFile(fileId)

        return file
      })
    )

    if (files.length === 1) {
      if (files[0].type === FileType.Folder) {
        resolve.set([FileBrowserResolveType.File, files[0].id])
        return
      }

      onresult([files[0]])
    } else {
      const filtered = files.filter((file) => file.type === FileType.Folder)

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
