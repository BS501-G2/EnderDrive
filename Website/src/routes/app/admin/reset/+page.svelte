<script lang="ts">
  import { useServerContext } from '$lib/client/client'
  import { useAppContext } from '$lib/client/contexts/app'
  import Table from '$lib/client/ui/table.svelte'

  const { getPasswordResetRequests } = useServerContext()
  const { isDesktop } = useAppContext()
</script>

<Table>
  {#if $isDesktop}
  <thead>
    <tr>
      <th></th>
    </tr>
  </thead>
  {/if}
  <thead>
    <tr></tr>
  </thead>
</Table>
