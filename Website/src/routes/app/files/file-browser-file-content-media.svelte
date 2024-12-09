<script lang="ts">
  import { useClientContext } from '$lib/client/client'
  import type { FileDataResource, FileResource } from '$lib/client/resource'
  import LoadingSpinner from '$lib/client/ui/loading-spinner.svelte'
  import { generateFileTokenUrl } from '$lib/client/utils'
  import { type Writable } from 'svelte/store'

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
</script>

{#await generateFileTokenUrl(server, file, fileData)}
  <div class="loading">
    <LoadingSpinner size="3rem" />
  </div>
{:then url}
  <embed src={url} type={mime} />
{/await}

<style lang="scss">
  embed {
    flex-grow: 1;
  }

  div.loading {
    align-items: center;
    justify-content: center;
  }
</style>
