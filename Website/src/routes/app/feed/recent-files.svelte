<script lang="ts">
  import { FileType, useServerContext, type FileLogResource } from '$lib/client/client'
  import { useAppContext } from '$lib/client/contexts/app'
  import LoadingSpinner from '$lib/client/ui/loading-spinner.svelte'
  import RecentEntry from './recent-entry.svelte'

  const server = useServerContext()
  const { isDesktop } = useAppContext()

  async function load() {
    const logs = await server.getFileLogs({ count: 50 })

    return await Promise.all(
      logs.map(async (fileLog) => {
        console.log(fileLog)
        const file = await server.getFile(fileLog.fileId)

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
    {#each logs.toSorted((a, b) => new Date(b.fileLog.createTime).getTime() - new Date(a.fileLog.createTime).getTime()) as { file, fileLog } (file.id)}
      {#if file.type === FileType.File}
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
