<script lang="ts">
  import { useDashboardContext } from '$lib/client/contexts/dashboard'
  import { onMount, type Snippet } from 'svelte'
  import { useClientContext } from '$lib/client/client'
  import LoadingSpinner from '$lib/client/ui/loading-spinner.svelte'
  import { toReadableSize } from '$lib/client/utils'
  import Button from '$lib/client/ui/button.svelte'

  const { pushDesktopBottom } = useDashboardContext()
  const { server } = useClientContext()

  onMount(() => pushDesktopBottom(stats, 'right'))

  const maxWidth = 128
</script>

{#snippet stats()}
  <div class="stats">
    {#await (async () => {
      const { DiskUsage, DiskTotal } = await server.GetDiskUsage({})

      return { DiskUsage, DiskTotal }
    })()}
      <LoadingSpinner size="1rem" />
    {:then { DiskUsage, DiskTotal }}
      {@const DiskFree = DiskTotal - DiskUsage}

      {#snippet foreground(view: Snippet)}
        <div class="stats-inner">
          {@render view()}
        </div>
      {/snippet}

      <Button {foreground} onclick={() => {}} hint="Overall Server Storage of EnderDrive">
        <div class="bar">
          <div
            class="used"
            style:min-width="{(DiskUsage / DiskTotal) * maxWidth}px"
            style:max-width="{(DiskUsage / DiskTotal) * maxWidth}px"
          ></div>
          <div
            class="free"
            style:min-width="{(DiskFree / DiskTotal) * maxWidth}px"
            style:max-width="{(DiskFree / DiskTotal) * maxWidth}px"
          ></div>
        </div>

        <p>{toReadableSize(DiskFree)} free of {toReadableSize(DiskTotal)}</p>
      </Button>
    {/await}
  </div>
{/snippet}

<style lang="scss">
  div.stats {
    flex-direction: row;
    align-items: center;

    gap: 8px;
  }

  div.stats-inner {
    flex-direction: row;

    gap: 8px;

    padding: 0 4px;
  }

  div.bar {
    align-self: stretch;
    flex-direction: row;

    margin: 4px 0;
    border-radius: 8px;
    border: solid 1px var(--color-1);
    overflow: hidden;

    div.used {
      background-color: var(--color-1);
    }

    div.free {
      background-color: var(--color-5);
    }
  }
</style>
