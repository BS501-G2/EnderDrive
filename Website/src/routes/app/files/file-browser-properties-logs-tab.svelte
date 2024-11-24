<script lang="ts">
  import { FileLogType, useServerContext, type FileResource } from '$lib/client/client'
  import type { FileProperties } from '$lib/client/contexts/file-browser'
  import LoadingSpinner from '$lib/client/ui/loading-spinner.svelte'
  import FileBrowserPropertiesLogsTabEntry from './file-browser-properties-logs-tab-entry.svelte'
  import FileBrowserPropertiesTab from './file-browser-properties-tab.svelte'

  const { file }: { file: FileProperties } = $props()
  const server = useServerContext()
</script>

<FileBrowserPropertiesTab label="File Logs" icon={{ icon: 'book', thickness: 'solid' }}>
  {#await (async () => {
    const getFileLogs = await server.getFileLogs({ fileId: file.file.id })

    return { getFileLogs: getFileLogs.filter((e) => e.type !== FileLogType.Read) }
  })()}
    <div class="loading">
      <LoadingSpinner size="3rem" />
    </div>
  {:then { getFileLogs }}
    <div class="file-access">
      {#each getFileLogs as getFileLog}
        <FileBrowserPropertiesLogsTabEntry fileLog={getFileLog} />
      {/each}
    </div>
  {/await}
</FileBrowserPropertiesTab>

<style lang="scss">
  div.loading {
    flex-grow: 1;

    align-items: center;
    justify-content: center;
  }

  div.file-access {
    padding: 8px;
    gap: 8px;
  }
</style>
