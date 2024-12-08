<script lang="ts">
  import { useClientContext } from '$lib/client/client'
  import type { FileDataResource, FileResource } from '$lib/client/resource'
  import { bufferSize } from '$lib/client/utils'
  import { onMount } from 'svelte'
  import { writable, type Writable } from 'svelte/store'

  const {
    file,
    fileData,
    mime
  }: {
    mime: string
    file: FileResource
    fileData: FileDataResource
  } = $props()
  const { server } = useClientContext()

  const progress: Writable<[current: number, total: number]> = writable([0, 0])
  let mounted = $state(true)

  async function load(): Promise<string> {
    const streamId = await server.StreamOpen({
      FileId: file.Id,
      FileDataId: fileData.Id,
      ForWriting: false
    })
    const length = await server.StreamGetLength({ StreamId: streamId })

    let blob = new Blob()
    let offset = 0

    while (mounted) {
      const buffer = await server.StreamRead({
        StreamId: streamId,
        Length: bufferSize
      })

      blob = new Blob([blob, buffer], { type: mime })

      progress.set([offset, length])

      offset += buffer.byteLength
      if (!buffer.byteLength) {
        break
      }
    }

    await server.StreamClose({ StreamId: streamId })

    return URL.createObjectURL(blob)
  }

  let promise = writable(load())

  onMount(() => () => {
    mounted = false
  })
</script>

{#await $promise}
  <div class="loading">
    <div class="loading-message">
      <p>Downloading...</p>
      <progress value={$progress[0]} max={$progress[1]}></progress>
    </div>
  </div>
{:then imageUrl}
  <embed src={imageUrl} title="Image Content" />
{/await}

<style lang="scss">
  embed {
    flex-grow: 1;
  }

  div.loading {
    flex-grow: 1;
    gap: 8px;

    align-items: center;
    justify-content: center;
  }
</style>
