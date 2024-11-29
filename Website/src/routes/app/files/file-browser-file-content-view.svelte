<script lang="ts">
  import {
    useServerContext,
    type FileContentResource,
    type FileResource,
    type FileDataResource
  } from '$lib/client/client'
  import { writable, type Writable } from 'svelte/store'

  const {
    file,
    fileContent,
    fileSnapshot,
    mime
  }: {
    mime: string
    file: FileResource
    fileContent: FileContentResource
    fileSnapshot: FileDataResource
  } = $props()
  const { openStream, getStreamPosition, getStreamSize, closeStream, readStream } =
    useServerContext()

  const progress: Writable<[current: number, total: number]> = writable([0, 0])

  async function load(): Promise<string> {
    const streamId = await openStream(file.id, fileContent.id, fileSnapshot.id)
    const length = await getStreamSize(streamId)

    let blob = new Blob()
    let offset = 0

    while (offset < length) {
      const buffer = await readStream(streamId, 1024 * 1024)
      blob = new Blob([blob, buffer], { type: mime })
      offset += blob.size

      progress.set([offset, length])
    }

    await closeStream(streamId)

    return URL.createObjectURL(blob)
  }

  let promise = writable(load())
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
