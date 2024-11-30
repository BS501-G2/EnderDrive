<script lang="ts">
  import { type UserResource } from '$lib/client/resource';
  import { useAppContext } from '$lib/client/contexts/app'
  import Table from '$lib/client/ui/table.svelte'
  import UserEntry from './user-entry.svelte'

  const { users }: { users: UserResource[] } = $props()
  const { isDesktop } = useAppContext()
</script>

<Table>
  {#if $isDesktop}
    <thead>
      <tr class="head">
        <th></th>
        <th>User</th>
        <th>Roles</th>
        <th>Admin Access</th>
      </tr>
    </thead>
  {/if}

  <tbody>
    {#each users as user}
      <UserEntry {user} />
    {/each}
  </tbody>
</Table>

<style lang="scss">
  @use '../../../../global.scss' as *;

  tr.head {
    font-weight: bolder;
  }
</style>
