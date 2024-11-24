<script lang="ts">
  import { useServerContext } from '$lib/client/client'
  import type { FileProperties } from '$lib/client/contexts/file-browser'
  import { derived } from 'svelte/store'
  import FileBrowserPropertiesTab from './file-browser-properties-tab.svelte'
  import { page } from '$app/stores'

  const { file }: { file: FileProperties } = $props()

  const server = useServerContext()
</script>

<FileBrowserPropertiesTab label="File Revisions" icon={{ icon: 'history', thickness: 'solid' }}>
  {#await (async () => {
    const fileContent = await server.getMainFileContent(file.file.id)
    const fileSnapshots = await server.getFileSnapshots(file.file.id, fileContent.id)

    return { fileSnapshots }
  })() then { fileSnapshots }}
    {#each fileSnapshots as fileSnapshot}
      <a href="/app/files?fileId={file.file.id}&snapshotId={fileSnapshot.id}">ASd</a>
    {/each}
  {/await}
</FileBrowserPropertiesTab>
