<script lang="ts">
  import type { FileDataResource, FileResource } from '$lib/client/resource'
  import { writable } from 'svelte/store'
  import { useClientContext } from '$lib/client/client'

  const { file, fileData }: { file: FileResource; fileData: FileDataResource } = $props()

  const { server } = useClientContext()

  async function load(): Promise<string> {
    const streamId = await server.StreamOpen({
      FileId: file.Id,
      FileDataId: fileData.Id,
      ForWriting: false
    })

    let blob = new Blob()

    while (
      (await server.StreamGetPosition({ StreamId: streamId })) <
      (await server.StreamGetLength({ StreamId: streamId }))
    ) {
      const buffer = await server.StreamRead({
        StreamId: streamId,
        Length: 1024 * 1024
      })

      blob = new Blob([blob, buffer])
    }

    await server.StreamClose({ StreamId: streamId })

    return new TextDecoder().decode(await blob.arrayBuffer())
  }

  let promise = writable(load())
</script>

{#await $promise then imageUrl}
  <pre>{imageUrl}</pre>
{/await}
