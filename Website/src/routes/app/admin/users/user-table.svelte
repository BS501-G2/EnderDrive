<script lang="ts">
  import { useServerContext, type UserResource } from '$lib/client/client'
  import { useAppContext } from '$lib/client/contexts/app'
  import UserLink from '$lib/client/model/user-link.svelte'
  import Icon from '$lib/client/ui/icon.svelte'
  import LoadingSpinner from '$lib/client/ui/loading-spinner.svelte'
  import UserEntry from './user-entry.svelte'

  const { users }: { users: UserResource[] } = $props()
  const { amIAdmin, isUserAdmin } = useServerContext()
  const { isDesktop, isMobile } = useAppContext()
</script>

<table class="user-list">
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
</table>

<style lang="scss">
  @use '../../../../global.scss' as *;

  table.user-list {
    :global(td), :global(th) {
      align-content: center;
      justify-items: start;

      width: min-content;
    }

    tr.head {
      font-weight: bolder;

      th {
        padding: 8px 0 8px 0;
        text-align: start;
      }
    }

    tbody {
      overflow: hidden auto;
    }
  }
</style>
