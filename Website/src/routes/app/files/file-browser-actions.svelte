<script lang="ts">
  import {
    FileAccessLevel,
    FileType,
    useServerContext,
    type FileResource
  } from '$lib/client/client'
  import { useFileBrowserContext, type CurrentFile } from '$lib/client/contexts/file-browser'
  import { derived, writable, type Writable } from 'svelte/store'
  import FileBrowserAction from './file-browser-action.svelte'
  import FileBrowserCreateFolder from './file-browser-create-folder.svelte'
  import { useDashboardContext } from '$lib/client/contexts/dashboard'
  import { toReadableSize } from '$lib/client/utils'
  import { page } from '$app/stores'
  import FileRenameDialog from './file-rename-dialog.svelte'

  const {
    current,
    selectedFileIds
  }: {
    current: CurrentFile
    selectedFileIds: Writable<string[]>
  } = $props()
  const { refresh, selectMode, onFileId, stored } = useFileBrowserContext()
  const { executeBackgroundTask } = useDashboardContext()
  const {
    createFile,
    writeStream,
    truncateStream,
    closeStream,
    getFileStar,
    getFile,
    setFileStar,
    trashFile,
    untrashFile,
    openStream,
    getFileAccessLevel,
    getFileContents,
    getFileSnapshots,
    getLatestFileSnapshot,
    getMainFileContent
  } = useServerContext()
  const server = useServerContext()

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
  const starred = await Promise.all(selectedFileIds.map((fileId) => getFileStar(fileId)))
  const files = await Promise.all(selectedFileIds.map((fileId) => getFile(fileId)))
  const fileAccessLevel = await Promise.all(selectedFileIds.map( (fileId) => getFileAccessLevel(fileId) ))

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
          selectedFileIds.set(current.files.map((file) => file.file.id))
        }}
      />
    {/if}
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

              if (file.size > 1024 * 1024 * 10) {
                throw new Error('Files exceeds upload limit.')
              }

              const fileResource = await getFile(nonReactiveSelectedFileIds[0])
              const fileContent = await getMainFileContent(fileResource.id)
              let fileSnapshot = await getLatestFileSnapshot(fileResource.id, fileContent.id)

              const streamId = await openStream(fileResource.id, fileContent.id, fileSnapshot!.id)
              const newFileSnapshotId = await truncateStream(streamId, file.size)
              if (newFileSnapshotId != null) {
                fileSnapshot = (
                  await getFileSnapshots(streamId, fileContent.id, newFileSnapshotId, 0, 1)
                )[0]
              }

              const bufferSize = 1024 * 256

              setMessage(`${Math.round((uploadedLength / file.size) * 10000) / 100}%`)
              setFooterLeft(file.name)
              setFooterRight(null)

              for (let index = 0; index < file.size; ) {
                const buffer = file.slice(index, index + bufferSize)

                await writeStream(streamId, buffer)

                uploadedLength += buffer.size
                index += bufferSize

                setProgress([uploadedLength, file.size])
                setMessage(`${Math.round((uploadedLength / file.size) * 10000) / 100}%`)
                setFooterRight(
                  `${toReadableSize(Math.min(index, file.size))}/${toReadableSize(file.size)}`
                )
              }

              await closeStream(streamId)

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
          $stored = [current.file.id, nonReactiveSelectedFileIds]
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

          if ($stored[0] !== current.file.id) {
            for (const fileId of $stored[1]) {
              await server.moveFile({
                fileId,
                newParentId: current.file.id
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
          await server.deleteFile(fileId)
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

              if (files.some((file) => file.size > 1024 * 1024 * 10)) {
                throw new Error('Some of the exceeds upload limit.')
              }

              for (const file of files) {
                const streamId = await createFile(current.file.id, file.name)
                const bufferSize = 1024 * 256

                setMessage(`${Math.round((uploadedLength / totalLength) * 10000) / 100}%`)
                setFooterLeft(file.name)
                setFooterRight(null)

                for (let index = 0; index < file.size; ) {
                  const buffer = file.slice(index, index + bufferSize)

                  await writeStream(streamId, buffer)

                  uploadedLength += buffer.size
                  index += bufferSize

                  setProgress([uploadedLength, totalLength])
                  setMessage(`${Math.round((uploadedLength / totalLength) * 10000) / 100}%`)
                  setFooterRight(
                    // `${Math.round((Math.min(index, file.size) / file.size) * 10000) / 100}%`

                    `${toReadableSize(Math.min(index, file.size))}/${toReadableSize(file.size)}`
                  )
                }

                await closeStream(streamId)
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
    {#if files[0].trashTime == null}
      <FileBrowserAction
        type="left"
        icon={{
          icon: 'trash-can',
          thickness: 'regular'
        }}
        label="Trash"
        onclick={async (event) => {
          for (const fileId of nonReactiveSelectedFileIds) {
            await trashFile(fileId)
          }

          if (current.type === 'file') {
            onFileId?.(event, files[0].parentId)
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
            await untrashFile(fileId)
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
          const file = await server.getFile(nonReactiveSelectedFileIds[0])
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
          const fileContent = await server.getMainFileContent(nonReactiveSelectedFileIds[0])
          const fileSnapshot = (await server.getLatestFileSnapshot(
            nonReactiveSelectedFileIds[0],
            fileContent.id
          ))!

          const virus = await server.scanFile(
            nonReactiveSelectedFileIds[0],
            fileContent.id,
            $snapshotId ?? fileSnapshot.id
          )

          if (virus.viruses.length > 0) {
            throw new Error('Cannot download a file with viruses.')
          }

          const stream = await server.openStream(
            nonReactiveSelectedFileIds[0],
            fileContent.id,
            fileSnapshot.id
          )

          let data = new Blob([], { type: 'application/octet-stream' })
          for (let offset = 0; offset < fileSnapshot.size; offset += 1024 * 8) {
            const buffer = await server.readStream(stream, 1024 * 8)

            data = new Blob([data, buffer], { type: 'application/octet-stream' })
          }
          await server.closeStream(stream)

          window.open(URL.createObjectURL(data))
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
          await setFileStar(fileId, !isStarred)
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
  />
{/if}
