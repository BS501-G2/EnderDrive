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

  const { pushTitle } = useAdminContext()

  onMount(() => pushTitle('File Logs'))
  const { server } = useClientContext()

  const logs = writable<FileLogResource[]>([])

  function load(div: HTMLDivElement) {
    const run = async () => {
      while (true) {
        if ($logs.length !== 0 && div.scrollTop / div.scrollWidth < 0.75) {
          return
        }

        const fileLogs = await server.GetFileLogs({
          Pagination: { Offset: $logs.length, Count: 75 },
          UniqueFileId: true
        })

        logs.update((logs) => {
          logs.push(...fileLogs)
          return logs
        })
      }
    }

    return promise.set(run().finally(() => promise.set(null)))
  }

  const promise = writable<Promise<void> | null>()

  const a = writable<HTMLDivElement>(null as never)

  onMount(() => load($a))
</script>

<LogFilter />

<div
  bind:this={$a}
  class="logs"
  onscroll={({ currentTarget }) => {
    load(currentTarget)
  }}
>
  {#each $logs as fileLog}
    <LogEntry {fileLog} />
  {/each}

  {#await $promise}
    <div class="loading">
      <LoadingSpinner size="3rem" />
    </div>
  {/await}
</div>

<style lang="scss">
  div.loading {
    flex-grow: 1;

    align-items: center;
    justify-content: center;
  }

  div.logs {
    padding: 16px;
    gap: 16px;
  }
</style>
