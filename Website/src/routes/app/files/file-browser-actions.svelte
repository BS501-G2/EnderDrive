<script lang="ts">
  import { useServerContext } from '$lib/client/client'
  import { useFileBrowserContext, type CurrentFile } from '$lib/client/contexts/file-browser'
  import type { Writable } from 'svelte/store'
  import FileBrowserAction from './file-browser-action.svelte'
  import FileBrowserCreateFolder from './file-browser-create-folder.svelte'

  const {
    current,
    selectedFileIds
  }: {
    current: CurrentFile
    selectedFileIds: Writable<string[]>
  } = $props()
  const { refresh, selectMode } = useFileBrowserContext()
  const { createFile, writeStream, closeStream, getFileStar, setFileStar } = useServerContext()

  let newFolder: boolean = $state(false)
  let uploadElement: HTMLInputElement & {
    type: 'file'
  } = $state(null as never)
  let uploadPromise: {
    resolve: (data: File[]) => void
  } | null = $state(null)
</script>

{#if current.type === 'folder' && selectMode == null}
  <FileBrowserAction
    type="left-main"
    icon={{
      icon: 'plus',
      thickness: 'solid'
    }}
    label="New Folder"
    onclick={() => {
      newFolder = true
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
      uploadElement.click()

      let files: File[]

      try {
        files = await new Promise((resolve) => {
          uploadPromise = {
            resolve
          }
        })

        if (files.length === 0) {
          return
        }

        for (const file of files) {
          const streamId = await createFile(current.file.id, file.name)
          const bufferSize = 1024 * 256

          for (let index = 0; index < file.size; index += bufferSize) {
            const buffer = file.slice(index, index + bufferSize)

            await writeStream(streamId, buffer)
          }

          await closeStream(streamId)
        }

        refresh()
      } finally {
        uploadPromise = null
      }
    }}
  />

  <input
    type="file"
    hidden
    multiple
    bind:this={uploadElement as never}
    onchange={({ currentTarget }) => {
      const files = Array.from(currentTarget.files ?? [])

      if (files.length === 0) {
        return
      }

      uploadPromise?.resolve(files)
    }}
    oncancel={() => {
      uploadPromise?.resolve([])
    }}
  />

  {#if newFolder}
    <FileBrowserCreateFolder
      parentFolder={current.file}
      ondismiss={() => {
        newFolder = false
      }}
    />
  {/if}
{/if}

{#if $selectedFileIds.length > 0}
  <FileBrowserAction
    type="left"
    icon={{
      icon: 'trash-can',
      thickness: 'regular'
    }}
    label="Delete"
    onclick={() => {}}
  />

  <FileBrowserAction
    type="left"
    icon={{
      icon: 'pencil',
      thickness: 'solid'
    }}
    label="Rename"
    onclick={() => {}}
  />

  <FileBrowserAction
    type="left"
    icon={{
      icon: 'download',
      thickness: 'solid'
    }}
    label="Download"
    onclick={() => {}}
  />
{/if}


{#if $selectedFileIds.length > 0}
  {#await Promise.all($selectedFileIds.map(async (fileId) => {
      return await getFileStar(fileId)
    })) then result}
    {@const starred = result.some((file) => file)}

    <FileBrowserAction
      type="left"
      icon={starred
        ? {
            icon: 'star',
            thickness: 'solid'
          }
        : {
            icon: 'star',
            thickness: 'regular'
          }}
      label={starred ? 'Unstar' : 'Star'}
      onclick={async () => {
        const starred = result.some((file) => file)
        await Promise.all(
          $selectedFileIds.map((fileId) => {
            console.log(fileId, starred)
            return setFileStar(fileId, !starred)
          })
        )
        refresh()
      }}
    />
  {/await}
{/if}
