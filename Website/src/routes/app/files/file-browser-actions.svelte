<script lang="ts">
  import { useFileBrowserContext, type CurrentFile } from '$lib/client/contexts/file-browser'
  import { derived, writable, type Writable } from 'svelte/store'
  import FileBrowserAction from './file-browser-action.svelte'
  import FileBrowserCreateFolder from './file-browser-create-folder.svelte'
  import { useDashboardContext } from '$lib/client/contexts/dashboard'
  import { bufferSize, toReadableSize } from '$lib/client/utils'
  import { page } from '$app/stores'
  import FileRenameDialog from './file-rename-dialog.svelte'
  import { useClientContext } from '$lib/client/client'
  import { FileAccessLevel, type FileResource } from '$lib/client/resource'
  import FileBrowserActionShare from './file-browser-action-share.svelte'
  import { useAppContext } from '$lib/client/contexts/app'

  const {
    current,
    selectedFileIds
  }: {
    current: CurrentFile
    selectedFileIds: Writable<string[]>
  } = $props()
  const { refresh, selectMode, onFileId, stored } = useFileBrowserContext()
  const { executeBackgroundTask } = useDashboardContext()
  const { isDesktop } = useAppContext()
  const { server } = useClientContext()

  const snapshotId = derived(page, ({ url }) => url.searchParams.get('snapshotId'))

  const newFolder = writable(false)
  const uploadElement = writable<
    HTMLInputElement & {
      type: 'file'
    }
  >(null as never)
  const uploadPromise = writable<{
    resolve: (data: File[]) => void
  } | null>(null)

  const updateElement: Writable<HTMLInputElement> = writable(null as never)
  const updatePromise = writable<{
    resolve: (data: File | null) => void
  } | null>(null)

  const renameDialog = writable<FileResource | null>(null)
</script>

{#await (async (selectedFileIds) => {
  const starred = await Promise.all(selectedFileIds.map( (fileId) => server.GetFileStar( { FileId: fileId } ) ))
  const files = await Promise.all(selectedFileIds.map( (fileId) => server.GetFile( { FileId: fileId } ) ))
  const fileAccessLevel = await Promise.all(selectedFileIds.map( (fileId) => server.GetFileAccessLevel( { FileId: fileId } ) ))

  return { nonReactiveSelectedFileIds: selectedFileIds, starred, files, fileAccessLevel }
})($selectedFileIds) then { starred, nonReactiveSelectedFileIds, files, fileAccessLevel }}
  {#if current.type !== 'file' && current.type !== 'error' && current.type !== 'loading'}
    {#if files.length != 0 && nonReactiveSelectedFileIds.length >= current.files.length}
      <FileBrowserAction
        type="left-main"
        label="Deselect All"
        icon={{
          icon: 'circle-check'
        }}
        onclick={() => {
          selectedFileIds.set([])
        }}
      />
    {:else if nonReactiveSelectedFileIds.length > 0}
      <FileBrowserAction
        type="left-main"
        label="Select All"
        icon={{
          icon: 'circle-check'
        }}
        onclick={() => {
          selectedFileIds.set(current.files.map((file) => file.file.Id))
        }}
      />
    {/if}
  {/if}

  {#if current.type == 'file' || current.type == 'folder' && $isDesktop}
    <FileBrowserActionShare file={$state.snapshot(current.file)} />
  {/if}


  {#if nonReactiveSelectedFileIds.length === 1 && fileAccessLevel[0] >= FileAccessLevel.ReadWrite}
    <FileBrowserAction
      type="left-main"
      label="Update File"
      icon={{
        icon: 'upload',
        thickness: 'solid'
      }}
      onclick={async () => {
        $updateElement.click()

        await executeBackgroundTask(
          'File Update',
          async ({ setMessage, setFooterLeft, setFooterRight, setProgress, setTitle }) => {
            try {
              const file = await new Promise<File | null>((resolve) => {
                $updatePromise = {
                  resolve
                }
              })

              let uploadedLength: number = 0

              if (file == null) {
                return
              }

              const fileResource = await server.GetFile({ FileId: nonReactiveSelectedFileIds[0] })
              const fileData = await server
                .FileGetDataEntries({
                  FileId: fileResource.Id,
                  Pagination: { Count: 1 }
                })
                .then((result) => result[0])

              const streamId = await server.StreamOpen({
                FileId: fileResource.Id,
                FileDataId: fileData.Id,
                ForWriting: true
              })

              setMessage(`${Math.round((uploadedLength / file.size) * 10000) / 100}%`)
              setFooterLeft(file.name)
              setFooterRight(null)

              for (let index = 0; index < file.size; ) {
                const buffer = file.slice(index, index + bufferSize)

                await server.StreamWrite({ StreamId: streamId, Data: buffer })

                uploadedLength += buffer.size
                index += bufferSize

                setProgress([uploadedLength, file.size])
                setMessage(`${Math.round((uploadedLength / file.size) * 10000) / 100}%`)
                setFooterRight(
                  `${toReadableSize(Math.min(index, file.size))}/${toReadableSize(file.size)}`
                )
              }

              await server.StreamClose({ StreamId: streamId })

              refresh()
            } finally {
              $updatePromise = null
            }
          }
        )
      }}
    />
  {/if}

  {#if current.type === 'folder' && selectMode == null}
    {#if $stored == null && nonReactiveSelectedFileIds.length > 0}
      <FileBrowserAction
        type="left"
        icon={{
          icon: 'circle-arrow-right',
          thickness: 'solid'
        }}
        label="Move"
        onclick={() => {
          $stored = [current.file.Id, nonReactiveSelectedFileIds]
        }}
      />
    {:else if $stored != null}
      <FileBrowserAction
        type="left"
        icon={{
          icon: 'clipboard',
          thickness: 'regular'
        }}
        label="Paste"
        onclick={async () => {
          if ($stored == null) {
            return
          }

          if ($stored[0] !== current.file.Id) {
            for (const fileId of $stored[1]) {
              await server.MoveFile({
                FileId: fileId,
                NewParentId: current.file.Id
              })
            }
          }

          refresh?.()
          $stored = null
        }}
      />

      <FileBrowserAction
        type="left"
        icon={{
          icon: 'x',
          thickness: 'solid'
        }}
        label="Cancel"
        onclick={async () => {
          $stored = null
        }}
      />
    {/if}
  {/if}

  {#if current.type === 'trash' && nonReactiveSelectedFileIds.length > 0}
    <FileBrowserAction
      type="left-main"
      icon={{
        icon: 'trash-can',
        thickness: 'solid'
      }}
      label="Permanently Delete"
      onclick={async () => {
        for (const fileId of nonReactiveSelectedFileIds) {
          await server.FileDelete({ FileId: fileId })
        }

        refresh?.()
      }}
    />
  {/if}

  {#if current.type === 'folder' && selectMode == null && nonReactiveSelectedFileIds.length === 0}
    <FileBrowserAction
      type="left-main"
      icon={{
        icon: 'plus',
        thickness: 'solid'
      }}
      label="New Folder"
      onclick={() => {
        $newFolder = true
      }}
    />

    <FileBrowserAction
      type="left-main"
      icon={{
        icon: 'plus',
        thickness: 'solid'
      }}
      label="Upload"
      onclick={async () => {
        $uploadElement.click()

        await executeBackgroundTask(
          'File Upload',
          async ({ setMessage, setFooterLeft, setFooterRight, setProgress, setTitle }) => {
            try {
              const files: File[] = await new Promise<File[]>((resolve) => {
                $uploadPromise = {
                  resolve
                }
              })

              const totalLength = files.reduce((total, file) => total + file.size, 0)
              let uploadedLength: number = 0

              if (files.length === 0) {
                return
              }

              for (const file of files) {
                const streamId = await server.FileCreate({
                  FileId: current.file.Id,
                  Name: file.name
                })

                setMessage(`${Math.round((uploadedLength / totalLength) * 10000) / 100}%`)
                setFooterLeft(file.name)
                setFooterRight(null)

                for (let index = 0; index < file.size; ) {
                  const buffer = file.slice(index, index + bufferSize)

                  await server.StreamWrite({
                    StreamId: streamId,
                    Data: buffer
                  })

                  uploadedLength += buffer.size
                  index += bufferSize

                  setProgress([uploadedLength, totalLength])
                  setMessage(`${Math.round((uploadedLength / totalLength) * 10000) / 100}%`)
                  setFooterRight(
                    // `${Math.round((Math.min(index, file.size) / file.size) * 10000) / 100}%`

                    `${toReadableSize(Math.min(index, file.size))}/${toReadableSize(file.size)}`
                  )
                }

                await server.StreamClose({ StreamId: streamId })
              }

              refresh()
            } finally {
              $uploadPromise = null
            }
          }
        )
      }}
    />

    {#if $newFolder}
      <FileBrowserCreateFolder
        parentFolder={current.file}
        ondismiss={() => {
          $newFolder = false
        }}
      />
    {/if}
  {/if}

  {#if nonReactiveSelectedFileIds.length > 0}
    {#if files[0].TrashTime == null}
      <FileBrowserAction
        type="left"
        icon={{
          icon: 'trash-can',
          thickness: 'regular'
        }}
        label="Trash"
        onclick={async (event) => {
          for (const fileId of nonReactiveSelectedFileIds) {
            await server.TrashFile({ FileId: fileId })
          }

          if (current.type === 'file') {
            onFileId?.(event, files[0].ParentId || null)
          } else {
            refresh()
          }
        }}
      />
    {:else}
      <FileBrowserAction
        type="left"
        icon={{
          icon: 'trash-can',
          thickness: 'solid'
        }}
        label="Restore"
        onclick={async (event) => {
          for (const fileId of nonReactiveSelectedFileIds) {
            await server.UntrashFile({ FileId: fileId })
          }

          refresh()
        }}
      />
    {/if}

    {#if nonReactiveSelectedFileIds.length === 1}
      <FileBrowserAction
        type="left"
        icon={{
          icon: 'pencil',
          thickness: 'solid'
        }}
        label="Rename"
        onclick={async () => {
          const file = await server.GetFile({ FileId: nonReactiveSelectedFileIds[0] })
          $renameDialog = file
        }}
      />

      <FileBrowserAction
        type="left"
        icon={{
          icon: 'download',
          thickness: 'solid'
        }}
        label="Download"
        onclick={async () => {
          const fileData = await server
            .FileGetDataEntries({
              FileId: nonReactiveSelectedFileIds[0],
              FileDataId: $page.url.searchParams.get('dataId') ?? undefined,
              Pagination: { Count: 1 }
            })
            .then((result) => result[0])

          const fileDataSize = await server.FileGetSize({
            FileId: nonReactiveSelectedFileIds[0],
            FileDataId: fileData.Id
          })

          const virus = await server.FileScan({
            FileId: nonReactiveSelectedFileIds[0],
            FileDataId: fileData.Id
          })

          if ((virus.Viruses?.length ?? 0) > 0) {
            throw new Error('Cannot download a file with viruses.')
          }

          const streamId = await server.StreamOpen({
            FileId: nonReactiveSelectedFileIds[0],
            FileDataId: fileData.Id,
            ForWriting: false
          })

          let data = new Blob([], { type: 'application/octet-stream' })
          for (let offset = 0; offset < fileDataSize; offset += bufferSize) {
            const buffer = await server.StreamRead({
              StreamId: streamId,
              Length: bufferSize
            })

            data = new Blob([data, buffer], { type: 'application/octet-stream' })
          }

          await server.StreamClose({ StreamId: streamId })

          const url = URL.createObjectURL(data)

          window.open(url, '_blank')
        }}
      />
    {/if}
  {/if}

  {#if nonReactiveSelectedFileIds.length > 0}
    {@const isStarred = starred.some((file) => file)}

    <FileBrowserAction
      type="left"
      icon={isStarred
        ? {
            icon: 'star',
            thickness: 'solid'
          }
        : {
            icon: 'star',
            thickness: 'regular'
          }}
      label={isStarred ? 'Unstar' : 'Star'}
      onclick={async () => {
        const isStarred = starred.some((file) => file)

        for (const fileId of nonReactiveSelectedFileIds) {
          await server.SetFileStar({
            FileId: fileId,
            Starred: !isStarred
          })
        }

        refresh()
      }}
    />
  {/if}
{/await}

<input
  hidden
  type="file"
  bind:this={$updateElement as never}
  onchange={({ currentTarget }) => {
    const file = currentTarget.files?.item(0)

    if (file == null) {
      return
    }

    $updatePromise?.resolve(file)
  }}
  oncancel={() => {
    $updatePromise?.resolve(null)
  }}
/>

<input
  type="file"
  hidden
  multiple
  bind:this={$uploadElement as never}
  onchange={({ currentTarget }) => {
    const files = Array.from(currentTarget.files ?? [])

    if (files.length === 0) {
      return
    }

    $uploadPromise?.resolve(files)
  }}
  oncancel={() => {
    $uploadPromise?.resolve([])
  }}
/>

{#if $renameDialog != null}
  <FileRenameDialog
    file={$renameDialog}
    ondismiss={() => {
      $renameDialog = null
    }}
    onresult={() => {
      refresh?.()
    }}
  />
{/if}
