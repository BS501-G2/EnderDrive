<script lang="ts">
  import { useAdminContext } from '$lib/client/contexts/admin'
  import { onMount } from 'svelte'
  import Title from '../../title.svelte'

  import LoadingSpinner from '$lib/client/ui/loading-spinner.svelte'
  import LogEntry from './log-entry.svelte'
  import { writable } from 'svelte/store'
  import LogFilter from './log-filter.svelte'
  import { useClientContext } from '$lib/client/client'
  import type { FileLogResource } from '$lib/client/resource'
  import LazyLoader from '$lib/client/ui/lazy-loader.svelte'

  const { pushTitle } = useAdminContext()

  onMount(() => pushTitle('File Logs'))
  const { server } = useClientContext()

  // function load(div: HTMLDivElement) {
  //   const run = async () => {
  //     while (true) {
  //       if ($logs.length !== 0 && div.scrollTop / div.scrollWidth < 0.75) {
  //         return
  //       }

  //       const fileLogs = await server.GetFileLogs({
  //         Pagination: { Offset: $logs.length, Count: 75 },
  //         UniqueFileId: true
  //       })

  //       logs.update((logs) => {
  //         logs.push(...fileLogs)
  //         return logs
  //       })
  //     }
  //   }

  //   return promise.set(run().finally(() => promise.set(null)))
  // }

  const promise = writable<Promise<void> | null>()

  const a = writable<HTMLDivElement>(null as never)

  let logs = $state<FileLogResource[]>([])

  // onMount(() => load($a))
</script>

<LogFilter />

<div class="logs">
  <LazyLoader
    class="test"
    bind:items={logs}
    load={async (offset) => {
      return await server.GetFileLogs({
        Pagination: { Count: 75, Offset: offset },
        UniqueFileId: false
      })
    }}
    vertical
  >
    {#snippet itemSnippet(item, index, key)}
      <LogEntry fileLog={item} />
    {/snippet}
  </LazyLoader>
  {logs.length}
</div>

<style lang="scss">
  div.logs {
    flex-grow: 1;

    padding: 16px;
    gap: 16px;
  }

  div.test {
    padding: 16px;
  }
</style>
