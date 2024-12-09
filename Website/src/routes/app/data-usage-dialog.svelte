<script lang="ts">
  import { useClientContext } from '$lib/client/client'
  import type { UserResource } from '$lib/client/resource'
  import Window from '$lib/client/ui/window.svelte'
  import { toReadableSize } from '$lib/client/utils'

  const { server } = useClientContext()
  const { user, ondismiss }: { user: UserResource; ondismiss: () => void } = $props()
</script>

<Window title="Data Usage for @{user.Username}" {ondismiss}>
  {#await (async () => {
    const { FileCount, DiskUsage } = await server.GetUserDiskUsage({ UserId: user.Id })

    return { FileCount, DiskUsage }
  })()}
    <!-- // -->
  {:then { FileCount, DiskUsage }}
    <div class="row"><b class="label">Total Files</b> <i class="value">{FileCount}</i></div>
    <div class="row">
      <b class="label">Total Size</b> <i class="value">{toReadableSize(DiskUsage)}</i>
    </div>
  {/await}
</Window>

<style lang="scss">
  div.row {
    flex-direction: row;
  }

  b.label {
    flex-grow: 1;
    flex-basis: 0;

    font-weight: bolder;
  }

  b.label::after {
    content: ':';
  }

  i.value {
    flex-grow: 1;
    flex-basis: 0;

    text-align: end;
  }
</style>
