<script lang="ts">
  import {
    useServerContext,
    type FileContentResource,
    type FileResource,
    type FileDataResource
  } from '$lib/client/client'
  import { writable } from 'svelte/store'

  const {
    file,
    fileContent,
    fileSnapshot
  }: { file: FileResource; fileContent: FileContentResource; fileSnapshot: FileDataResource } =
    $props()
  const { openStream, getStreamPosition, getStreamSize, closeStream, readStream } =
    useServerContext()

  async function load(): Promise<string> {
    const streamId = await openStream(file.id, fileContent.id, fileSnapshot.id)
    let blob = new Blob()

    while ((await getStreamPosition(streamId)) < (await getStreamSize(streamId))) {
      const buffer = await readStream(streamId, 1024 * 1024)
      blob = new Blob([blob, buffer])
    }

    await closeStream(streamId)

    return new TextDecoder().decode(await blob.arrayBuffer());
  }

  let promise = writable(load())
</script>

{#await $promise}

{:then imageUrl}
<pre>{imageUrl}</pre>
{/await}
