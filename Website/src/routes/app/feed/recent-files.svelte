<script lang="ts">
  import { useClientContext } from '$lib/client/client'
  import { useAppContext } from '$lib/client/contexts/app'
  import { FileType } from '$lib/client/resource'
  import LoadingSpinner from '$lib/client/ui/loading-spinner.svelte'
  import RecentEntry from './recent-entry.svelte'

  const { server } = useClientContext()
  const { isDesktop } = useAppContext()

  async function load() {
    const logs = await server.GetFileLogs({ Pagination: { Count: 50 }, UniqueFileId: true })

    return await Promise.all(
      logs.map(async (fileLog) => {

        const file = await server.GetFile({FileId: fileLog.FileId})

        return { file, fileLog }
      })
    )
  }
</script>

<div class="list">
  {#if $isDesktop}
    <RecentEntry head />
  {/if}

  {#await load()}
    <div class="loading">
      <LoadingSpinner size="3em" />
    </div>
  {:then logs}
    {#each logs.toSorted((a, b) => new Date(b.fileLog.CreateTime).getTime() - new Date(a.fileLog.CreateTime).getTime()) as { file, fileLog }}
      {#if file.Type === FileType.File}
        <RecentEntry {file} {fileLog} />
      {/if}
    {/each}
  {/await}
</div>

<style lang="scss">
  div.loading {
    align-items: center;
    justify-content: center;
  }
</style>
