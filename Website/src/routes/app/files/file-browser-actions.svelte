<script lang="ts">
  import { useServerContext } from '$lib/client/client'
  import { useFileBrowserContext, type CurrentFile } from '$lib/client/contexts/file-browser'
  import { writable, type Writable } from 'svelte/store'
  import FileBrowserAction from './file-browser-action.svelte'
  import FileBrowserCreateFolder from './file-browser-create-folder.svelte'
  import { useDashboardContext } from '$lib/client/contexts/dashboard'
  import { toReadableSize } from '$lib/client/utils'

  const {
    current,
    selectedFileIds
  }: {
    current: CurrentFile
    selectedFileIds: Writable<string[]>
  } = $props()
  const { refresh, selectMode } = useFileBrowserContext()
  const { executeBackgroundTask } = useDashboardContext()
  const { createFile, writeStream, closeStream, getFileStar, setFileStar } = useServerContext()

  const newFolder = writable(false)
  const uploadElement = writable<
    HTMLInputElement & {
      type: 'file'
    }
  >(null as never)
  const uploadPromise = writable<{
    resolve: (data: File[]) => void
  } | null>(null)
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

  {#if $newFolder}
    <FileBrowserCreateFolder
      parentFolder={current.file}
      ondismiss={() => {
        $newFolder = false
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
