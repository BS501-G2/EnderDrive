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

  // let videoElement: HTMLVideoElement | null = $state(null)

  let url: string | null = $state(null)

  async function updateSource() {
    url = `http${window.location.protocol === 'https:' ? 's' : ''}://${window.location.host}/api/files/${await server.GenerateFileToken({
      FileId: file.Id,
      FileDataId: fileData.Id
    })}`

    console.log(url)
  }

  $effect(() => {
    void updateSource()
  })
</script>

{#if url != null}
  <embed src={url} type={mime} />
{/if}

<style lang="scss">
  embed {
    flex-grow: 1;
  }
</style>
